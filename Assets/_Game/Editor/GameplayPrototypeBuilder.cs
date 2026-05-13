using System.Collections.Generic;
using System.Linq;
using ThisIsBlast.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThisIsBlast.EditorTools
{
    public static class GameplayPrototypeBuilder
    {
        private const string RootPath = "Assets/_Game";
        private const string ScenePath = RootPath + "/Scenes/Gameplay.unity";
        private const string BlockPrefabPath = RootPath + "/Prefabs/Board/Block.prefab";
        private const string BlastItemPrefabPath = RootPath + "/Prefabs/Blast/BlastItem.prefab";
        private const string Level01Path = RootPath + "/ScriptableObjects/Levels/Level_01.asset";
        private const string Level02Path = RootPath + "/ScriptableObjects/Levels/Level_02.asset";

        private const string DefaultBlockModelPath = "Assets/Models/Cube1.obj";
        private const string DefaultBlastItemModelPath = "Assets/Models/Ball.obj";

        [MenuItem("Tools/This Is Blast/Build Gameplay Prototype")]
        public static void Build()
        {
            EnsureProjectFolders();

            Block blockPrefab = CreateBlockPrefab();
            BlastItem blastItemPrefab = CreateBlastItemPrefab();
            LevelData levelData = CreateLevel01();
            CreateLevel02();

            CreateGameplayScene(levelData, blockPrefab, blastItemPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("3D gameplay prototype generated. Open Assets/_Game/Scenes/Gameplay.unity and press Play.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        [MenuItem("Tools/This Is Blast/Build Gameplay Prototype Level 02")]
        public static void BuildLevel02()
        {
            EnsureProjectFolders();

            Block blockPrefab = CreateBlockPrefab();
            BlastItem blastItemPrefab = CreateBlastItemPrefab();
            CreateLevel01();
            LevelData levelData = CreateLevel02();

            CreateGameplayScene(levelData, blockPrefab, blastItemPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("3D gameplay prototype generated with Level 02. Open Assets/_Game/Scenes/Gameplay.unity and press Play.");
        }

        private static void EnsureProjectFolders()
        {
            EnsureFolder(RootPath + "/Scenes");
            EnsureFolder(RootPath + "/Scripts/Core");
            EnsureFolder(RootPath + "/Scripts/Board");
            EnsureFolder(RootPath + "/Scripts/Blast");
            EnsureFolder(RootPath + "/Scripts/Level");
            EnsureFolder(RootPath + "/Scripts/UI");
            EnsureFolder(RootPath + "/Scripts/Utils");
            EnsureFolder(RootPath + "/Prefabs/Board");
            EnsureFolder(RootPath + "/Prefabs/Blast");
            EnsureFolder(RootPath + "/ScriptableObjects/Levels");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static Block CreateBlockPrefab()
        {
            GameObject blockObject = new GameObject("Block");

            GameObject visual = CreateModelChild(DefaultBlockModelPath, "BlockModel", blockObject.transform, PrimitiveType.Cube);
            NormalizeVisualToUnitFootprint(visual, 0.92f);

            BoxCollider collider = blockObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.35f, 0f);
            collider.size = new Vector3(0.95f, 0.7f, 0.95f);

            BlockView blockView = blockObject.AddComponent<BlockView>();
            Block block = blockObject.AddComponent<Block>();

            SetObjectReference(block, "blockView", blockView);

            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(blockObject, BlockPrefabPath);
            Object.DestroyImmediate(blockObject);

            AssetDatabase.ImportAsset(BlockPrefabPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<GameObject>(BlockPrefabPath).GetComponent<Block>();
        }

        private static BlastItem CreateBlastItemPrefab()
        {
            GameObject itemObject = new GameObject("BlastItem");

            GameObject visual = CreateModelChild(DefaultBlastItemModelPath, "BlastItemModel", itemObject.transform, PrimitiveType.Sphere);
            NormalizeVisualToUnitFootprint(visual, 0.9f);

            BoxCollider collider = itemObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.35f, 0f);
            collider.size = new Vector3(0.95f, 0.7f, 0.95f);

            BlastItemView itemView = itemObject.AddComponent<BlastItemView>();
            BlastItem item = itemObject.AddComponent<BlastItem>();
            itemObject.AddComponent<BlastDragController>();

            TextMesh textMesh = CreatePowerText(itemObject.transform);

            SetObjectReference(itemView, "powerText", textMesh);
            SetObjectReference(item, "itemView", itemView);

            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(itemObject, BlastItemPrefabPath);
            Object.DestroyImmediate(itemObject);

            AssetDatabase.ImportAsset(BlastItemPrefabPath, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<GameObject>(BlastItemPrefabPath).GetComponent<BlastItem>();
        }

        private static GameObject CreateModelChild(
            string assetPath,
            string childName,
            Transform parent,
            PrimitiveType fallbackPrimitive)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            GameObject instance;

            if (modelAsset != null)
            {
                instance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                if (instance == null)
                {
                    instance = Object.Instantiate(modelAsset);
                }
            }
            else
            {
                instance = GameObject.CreatePrimitive(fallbackPrimitive);
            }

            instance.name = childName;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            foreach (Collider childCollider in instance.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(childCollider);
            }

            return instance;
        }

        private static void NormalizeVisualToUnitFootprint(GameObject visual, float targetFootprint)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = GetRendererBounds(renderers);
            float maxHorizontalSize = Mathf.Max(bounds.size.x, bounds.size.z);
            float maxAnySize = Mathf.Max(maxHorizontalSize, bounds.size.y);
            float size = Mathf.Max(maxAnySize, 0.001f);
            float scaleFactor = targetFootprint / size;
            visual.transform.localScale *= scaleFactor;

            bounds = GetRendererBounds(renderers);
            Vector3 offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            visual.transform.position += offset;
        }

        private static Bounds GetRendererBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static TextMesh CreatePowerText(Transform parent)
        {
            GameObject textObject = new GameObject("PowerText");
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = new Vector3(0f, 1.0f, -0.08f);
            textObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = "0";
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 96;
            textMesh.characterSize = 0.075f;
            textMesh.color = Color.white;

            MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
            textRenderer.sortingOrder = 20;

            return textMesh;
        }

        private static LevelData CreateLevel01()
        {
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(Level01Path);
            if (levelData == null)
            {
                levelData = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(levelData, Level01Path);
            }

            levelData.SetLevelInfo("level_01", 1);
            levelData.Initialize(10, 8, BlockColor.Yellow);
            levelData.SetWinCondition(WinConditionType.ClearAllBlocks);
            levelData.SetStartingBlastItems(
                new BlastItemConfig(BlockColor.Yellow, 44),
                new BlastItemConfig(BlockColor.Red, 60));

            for (int y = 0; y < levelData.Height; y++)
            {
                for (int x = 0; x < levelData.Width; x++)
                {
                    BlockColor color = x < levelData.Width / 2 ? BlockColor.Yellow : BlockColor.Red;
                    levelData.SetBlock(x, y, color);
                }
            }

            EditorUtility.SetDirty(levelData);
            return levelData;
        }

        private static LevelData CreateLevel02()
        {
            LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(Level02Path);
            if (levelData == null)
            {
                levelData = ScriptableObject.CreateInstance<LevelData>();
                AssetDatabase.CreateAsset(levelData, Level02Path);
            }

            levelData.SetLevelInfo("level_02", 2);
            levelData.Initialize(10, 8, BlockColor.Yellow);
            levelData.SetWinCondition(WinConditionType.ClearAllBlocks);
            levelData.SetStartingBlastItems(
                new BlastItemConfig(BlockColor.Yellow, 44),
                new BlastItemConfig(BlockColor.Red, 30),
                new BlastItemConfig(BlockColor.Blue, 30),
                new BlastItemConfig(BlockColor.Red, 30),
                new BlastItemConfig(BlockColor.Blue, 30),
                new BlastItemConfig(BlockColor.Green, 10));

            BlockColor[][] rows =
            {
                Row(BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow),
                Row(BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow),
                Row(BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue),
                Row(BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue),
                Row(BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow, BlockColor.Yellow),
                Row(BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue),
                Row(BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Red, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue, BlockColor.Blue),
                Row(BlockColor.Green, BlockColor.Green, BlockColor.Green, BlockColor.Green, BlockColor.Green, BlockColor.Green, BlockColor.Green, BlockColor.Green, BlockColor.Green, BlockColor.Green)
            };

            for (int y = 0; y < rows.Length; y++)
            {
                for (int x = 0; x < rows[y].Length; x++)
                {
                    levelData.SetBlock(x, y, rows[y][x]);
                }
            }

            EditorUtility.SetDirty(levelData);
            return levelData;
        }

        private static BlockColor[] Row(params BlockColor[] colors)
        {
            return colors;
        }

        private static void CreateGameplayScene(LevelData levelData, Block blockPrefab, BlastItem blastItemPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 7f, -7f);
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.14f);

            Light light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject bootstrapObject = new GameObject("GameBootstrap");
            GameBootstrap gameBootstrap = bootstrapObject.AddComponent<GameBootstrap>();

            GameObject boardObject = new GameObject("Board");
            BoardController boardController = boardObject.AddComponent<BoardController>();
            boardObject.AddComponent<BoardMatcher>();
            boardObject.AddComponent<BoardGravity>();

            Transform boardRoot = new GameObject("BoardRoot").transform;
            boardRoot.SetParent(boardObject.transform, false);

            GameObject levelObject = new GameObject("LevelController");
            LevelController levelController = levelObject.AddComponent<LevelController>();

            GameObject spawnerObject = new GameObject("BlastItemSpawner");
            BlastItemSpawner blastItemSpawner = spawnerObject.AddComponent<BlastItemSpawner>();
            Transform blastRoot = new GameObject("BlastItemsRoot").transform;
            blastRoot.SetParent(spawnerObject.transform, false);

            GameObject hudObject = new GameObject("GameplayHUD");
            GameplayHUD gameplayHUD = hudObject.AddComponent<GameplayHUD>();

            BoardMatcher boardMatcher = boardObject.GetComponent<BoardMatcher>();
            BoardGravity boardGravity = boardObject.GetComponent<BoardGravity>();

            SetObjectReference(gameBootstrap, "levelController", levelController);

            SetInt(boardController, "width", 10);
            SetInt(boardController, "height", 8);
            SetFloat(boardController, "cellSize", 0.65f);
            SetVector3(boardController, "originPosition", new Vector3(-2.925f, 0f, -1.0f));
            SetObjectReference(boardController, "blockPrefab", blockPrefab);
            SetObjectReference(boardController, "boardRoot", boardRoot);
            SetFloat(boardController, "blockFill", 0.92f);

            CreateBoardBackdrop(boardObject.transform, boardController);

            SetInt(levelController, "levelNumber", 1);
            SetObjectReference(levelController, "levelData", levelData);
            SetObjectReference(levelController, "boardController", boardController);
            SetObjectReference(levelController, "blastItemSpawner", blastItemSpawner);
            SetObjectReference(levelController, "gameplayHUD", gameplayHUD);

            SetObjectReference(blastItemSpawner, "blastItemPrefab", blastItemPrefab);
            SetObjectReference(blastItemSpawner, "boardController", boardController);
            SetObjectReference(blastItemSpawner, "boardMatcher", boardMatcher);
            SetObjectReference(blastItemSpawner, "boardGravity", boardGravity);
            SetObjectReference(blastItemSpawner, "levelController", levelController);
            SetObjectReference(blastItemSpawner, "spawnRoot", blastRoot);
            SetFloat(blastItemSpawner, "itemSpacing", 1.25f);
            SetFloat(blastItemSpawner, "boardBottomOffset", 1.25f);
            SetFloat(blastItemSpawner, "launcherSlotOffset", 0.62f);
            SetFloat(blastItemSpawner, "itemScale", 0.75f);
            SetInt(blastItemSpawner, "visibleItemCount", 3);
            SetFloat(blastItemSpawner, "fireInterval", 0.08f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
        }

        private static void CreateBoardBackdrop(Transform parent, BoardController boardController)
        {
            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "BoardBackdrop";
            backdrop.transform.SetParent(parent, false);

            float width = boardController.Width * boardController.CellSize + 0.35f;
            float depth = boardController.Height * boardController.CellSize + 0.35f;
            float centerX = boardController.OriginPosition.x + (boardController.Width - 1) * boardController.CellSize * 0.5f;
            float centerZ = boardController.OriginPosition.z + (boardController.Height - 1) * boardController.CellSize * 0.5f;

            backdrop.transform.position = new Vector3(centerX, boardController.BoardPlaneY - 0.08f, centerZ);
            backdrop.transform.localScale = new Vector3(width, 0.08f, depth);

            Renderer renderer = backdrop.GetComponent<Renderer>();
            renderer.sharedMaterial = new Material(Shader.Find("Standard"))
            {
                color = new Color(0.16f, 0.18f, 0.22f)
            };

            Object.DestroyImmediate(backdrop.GetComponent<Collider>());
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

            if (scenes.Any(existing => existing.path == scenePath))
            {
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError($"Missing serialized property '{propertyName}' on {target.name}.");
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError($"Missing serialized property '{propertyName}' on {target.name}.");
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError($"Missing serialized property '{propertyName}' on {target.name}.");
                return;
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector3(Object target, string propertyName, Vector3 value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogError($"Missing serialized property '{propertyName}' on {target.name}.");
                return;
            }

            property.vector3Value = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
