#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelSurvival
{
    public class EnemyCostumeCreatorWindow : EditorWindow
    {
        private GameObject _enemyPrefab;
        private EnemyType _enemyType;
        private float _scale = 1f;
        private Sprite _projectileSprite;
        private RuntimeAnimatorController _animController; 
        
        private string _costumeAssetName = "";

        [MenuItem("Tools/PixelSurvival/Enemy Costume Creator")]
        public static void Open()
        {
            GetWindow<EnemyCostumeCreatorWindow>("Enemy Costume Creator");
        }

        private void OnGUI()
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField("Enemy Costume Data Creator", EditorStyles.boldLabel);
            GUILayout.Space(4);

            _enemyPrefab = (GameObject)EditorGUILayout.ObjectField("Enemy Prefab", _enemyPrefab, typeof(GameObject), false);

            _costumeAssetName = EditorGUILayout.TextField("Costume SO Name", _costumeAssetName);

            _enemyType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", _enemyType);
            _scale = EditorGUILayout.FloatField("Scale", _scale);
            _projectileSprite = (Sprite)EditorGUILayout.ObjectField("Projectile Sprite", _projectileSprite, typeof(Sprite), false);
            _animController = (RuntimeAnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller", _animController, typeof(RuntimeAnimatorController), false);
            
            GUILayout.Space(10);

            using (new EditorGUI.DisabledScope(_enemyPrefab == null))
            {
                if (GUILayout.Button("Apply (Create EnemyCostumeData)", GUILayout.Height(32)))
                {
                    CreateCostumeData();
                }
            }

            GUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "프리팹 내부 SpriteRenderer를 이름으로 찾아 EnemyCostumeData에 매핑합니다.\n" +
                "Costume SO Name이 비어있으면 EnemyCostume_<PrefabName>.asset 로 생성됩니다.\n" +
                "Assets/08_DataSO 폴더에 생성됩니다.",
                MessageType.Info);
        }

        private void CreateCostumeData()
        {
            if (_enemyPrefab == null)
            {
                Debug.LogError("Enemy Prefab is null.");
                return;
            }

            string prefabPath = AssetDatabase.GetAssetPath(_enemyPrefab);
            if (string.IsNullOrEmpty(prefabPath))
            {
                prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_enemyPrefab);
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError("Prefab path not found. Please assign a prefab asset from Project view.");
                return;
            }

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var so = CreateInstance<EnemyCostumeData>();
                so.EnemyType = _enemyType;
                so.Scale = _scale;
                so.ProjectileSprite = _projectileSprite;
                so.AnimatorController = _animController; 
                
                // Body
                so.Head   = FindSprite(prefabRoot, "5_Head");
                so.Body   = FindSprite(prefabRoot, "Body");
                so.Arm_L  = FindSprite(prefabRoot, "20_L_Arm");
                so.Arm_R  = FindSprite(prefabRoot, "-20_R_Arm");
                so.Foot_L = FindSprite(prefabRoot, "_3L_Foot");
                so.Foot_R = FindSprite(prefabRoot, "_12R_Foot");

                // Equipment
                so.Hair      = FindSprite(prefabRoot, "7_Hair");
                
                so.Eye_L   = FindSpriteByNameThenPath(prefabRoot, "P_LEye", "PivotBack/Back");
                so.Pupil_L = FindSpriteByNameThenPath(prefabRoot, "P_LEye", "PivotFront/Front");
                so.Eye_R   = FindSpriteByNameThenPath(prefabRoot, "P_REye", "PivotBack/Back");
                so.Pupil_R = FindSpriteByNameThenPath(prefabRoot, "P_REye", "PivotFront/Front");

                so.Helmet           = FindSprite(prefabRoot, "12_Helmet2 ");
                so.Cloth            = FindSprite(prefabRoot, "ClothBody");
                so.ClothArm_L        = FindSprite(prefabRoot, "21_LCArm");
                so.ClothArm_R        = FindSprite(prefabRoot, "-19_RCArm");
                so.ClothShoulder_L   = FindSprite(prefabRoot, "25_L_Shoulder");
                so.ClothShoulder_R   = FindSprite(prefabRoot, "-15_R_Shoulder");

                so.Cape = FindSpriteByNameThenPath(prefabRoot, "P_Back", "Back");
                
                so.Armor            = FindSprite(prefabRoot, "BodyArmor");
                so.Weapon           = FindSprite(prefabRoot, "R_Weapon");
                so.Shield           = FindSprite(prefabRoot, "R_Shield");

                // 저장 폴더
                const string folder = "Assets/08_DataSO";
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    Directory.CreateDirectory(folder);
                    AssetDatabase.Refresh();
                }

                string baseName = string.IsNullOrWhiteSpace(_costumeAssetName)
                    ? $"EnemyCostume_{Path.GetFileNameWithoutExtension(prefabPath)}"
                    : _costumeAssetName.Trim();

                // 확장자 보장
                if (!baseName.EndsWith(".asset"))
                    baseName += ".asset";

                string assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(folder, baseName).Replace("\\", "/"));

                AssetDatabase.CreateAsset(so, assetPath);
                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Selection.activeObject = so;
                EditorGUIUtility.PingObject(so);

                Debug.Log($"EnemyCostumeData created: {assetPath}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Sprite FindSprite(GameObject root, string objectName)
        {
            Transform t = FindChildRecursive(root.transform, objectName);
            if (t == null)
            {
                Debug.LogWarning($"[EnemyCostumeCreator] Child not found: '{objectName}'");
                return null;
            }

            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                Debug.LogWarning($"[EnemyCostumeCreator] SpriteRenderer not found on: '{objectName}'");
                return null;
            }

            if (sr.sprite == null)
            {
                Debug.LogWarning($"[EnemyCostumeCreator] Sprite is null on: '{objectName}'");
                return null;
            }

            return sr.sprite;
        }
        
        private static Sprite FindSpriteByNameThenPath(GameObject root, string anchorName, string relativePath)
        {
            var anchor = FindChildRecursive(root.transform, anchorName);
            if (anchor == null)
            {
                Debug.LogWarning($"[EnemyCostumeCreator] Anchor not found: '{anchorName}'");
                return null;
            }

            var t = string.IsNullOrEmpty(relativePath) ? anchor : anchor.Find(relativePath);
            if (t == null)
            {
                Debug.LogWarning($"[EnemyCostumeCreator] Relative path not found: '{anchorName}/{relativePath}'");
                return null;
            }

            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
            {
                Debug.LogWarning($"[EnemyCostumeCreator] SpriteRenderer/Sprite missing: '{anchorName}/{relativePath}'");
                return null;
            }

            return sr.sprite;
        }


        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
