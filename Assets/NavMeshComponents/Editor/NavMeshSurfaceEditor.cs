#define NAVMESHCOMPONENTS_SHOW_NAVMESHDATA_REF

using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine.AI;
using UnityEngine;
using UnityEditor;
using UnityEditor.AI;
using System.Reflection;

namespace NavMeshPlus.Components.Editors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshSurface))]
    class NavMeshSurfaceEditor : Editor
    {
        SerializedProperty m_AgentTypeID;
        SerializedProperty m_BuildHeightMesh;
        SerializedProperty m_Center;
        SerializedProperty m_CollectObjects;
        SerializedProperty m_DefaultArea;
        SerializedProperty m_LayerMask;
        SerializedProperty m_OverrideTileSize;
        SerializedProperty m_OverrideVoxelSize;
        SerializedProperty m_Size;
        SerializedProperty m_TileSize;
        SerializedProperty m_UseGeometry;
        SerializedProperty m_VoxelSize;

#if NAVMESHCOMPONENTS_SHOW_NAVMESHDATA_REF
        SerializedProperty m_NavMeshData;
#endif
        class Styles
        {
            public readonly GUIContent m_LayerMask = new GUIContent("Include Layers");

            public readonly GUIContent m_ShowInputGeom = new GUIContent("Show Input Geom");
            public readonly GUIContent m_ShowVoxels = new GUIContent("Show Voxels");
            public readonly GUIContent m_ShowRegions = new GUIContent("Show Regions");
            public readonly GUIContent m_ShowRawContours = new GUIContent("Show Raw Contours");
            public readonly GUIContent m_ShowContours = new GUIContent("Show Contours");
            public readonly GUIContent m_ShowPolyMesh = new GUIContent("Show Poly Mesh");
            public readonly GUIContent m_ShowPolyMeshDetail = new GUIContent("Show Poly Mesh Detail");
        }

        static Styles s_Styles;

        static bool s_ShowDebugOptions;

        static Color s_HandleColor = new Color(127f, 214f, 244f, 100f) / 255;
        static Color s_HandleColorSelected = new Color(127f, 214f, 244f, 210f) / 255;
        static Color s_HandleColorDisabled = new Color(127f * 0.75f, 214f * 0.75f, 244f * 0.75f, 100f) / 255;

        BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

        bool editingCollider
        {
            get { return EditMode.editMode == EditMode.SceneViewEditMode.Collider && EditMode.IsOwner(this); }
        }

        void OnEnable()
        {
            m_AgentTypeID = serializedObject.FindProperty("m_AgentTypeID");
            m_BuildHeightMesh = serializedObject.FindProperty("m_BuildHeightMesh");
            m_Center = serializedObject.FindProperty("m_Center");
            m_CollectObjects = serializedObject.FindProperty("m_CollectObjects");
            m_DefaultArea = serializedObject.FindProperty("m_DefaultArea");
            m_LayerMask = serializedObject.FindProperty("m_LayerMask");
            m_OverrideTileSize = serializedObject.FindProperty("m_OverrideTileSize");
            m_OverrideVoxelSize = serializedObject.FindProperty("m_OverrideVoxelSize");
            m_Size = serializedObject.FindProperty("m_Size");
            m_TileSize = serializedObject.FindProperty("m_TileSize");
            m_UseGeometry = serializedObject.FindProperty("m_UseGeometry");
            m_VoxelSize = serializedObject.FindProperty("m_VoxelSize");

#if NAVMESHCOMPONENTS_SHOW_NAVMESHDATA_REF
            m_NavMeshData = serializedObject.FindProperty("m_NavMeshData");
#endif

        }


        Bounds GetBounds()
        {
            var navSurface = (NavMeshSurface)target;
            return new Bounds(navSurface.transform.position, navSurface.size);
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();

            var bs = NavMesh.GetSettingsByID(m_AgentTypeID.intValue);

            if (bs.agentTypeID != -1)
            {
                // Draw image
                const float diagramHeight = 80.0f;
                Rect agentDiagramRect = EditorGUILayout.GetControlRect(false, diagramHeight);
                NavMeshEditorHelpers.DrawAgentDiagram(agentDiagramRect, bs.agentRadius, bs.agentHeight, bs.agentClimb, bs.agentSlope);
            }

            EditorGUILayout.PropertyField(m_AgentTypeID);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_CollectObjects);
            if ((CollectObjects)m_CollectObjects.enumValueIndex == CollectObjects.Volume)
            {
                EditorGUI.indentLevel++;

                EditMode.DoEditModeInspectorModeButton(EditMode.SceneViewEditMode.Collider, "Edit Volume",
                    EditorGUIUtility.IconContent("EditCollider"), GetBounds, this);
                EditorGUILayout.PropertyField(m_Size);
                EditorGUILayout.PropertyField(m_Center);

                EditorGUI.indentLevel--;
            }
            else
            {
                if (editingCollider)
                    EditMode.QuitEditMode();
            }

            EditorGUILayout.PropertyField(m_LayerMask, s_Styles.m_LayerMask);
            EditorGUILayout.PropertyField(m_UseGeometry);

            EditorGUILayout.Space();

            m_OverrideVoxelSize.isExpanded = EditorGUILayout.Foldout(m_OverrideVoxelSize.isExpanded, "Advanced");
            if (m_OverrideVoxelSize.isExpanded)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(m_DefaultArea);

                // Override voxel size.
                EditorGUILayout.PropertyField(m_OverrideVoxelSize);

                using (new EditorGUI.DisabledScope(!m_OverrideVoxelSize.boolValue || m_OverrideVoxelSize.hasMultipleDifferentValues))
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(m_VoxelSize);

                    if (!m_OverrideVoxelSize.hasMultipleDifferentValues)
                    {
                        if (!m_AgentTypeID.hasMultipleDifferentValues)
                        {
                            float voxelsPerRadius = m_VoxelSize.floatValue > 0.0f ? (bs.agentRadius / m_VoxelSize.floatValue) : 0.0f;
                            EditorGUILayout.LabelField(" ", voxelsPerRadius.ToString("0.00") + " voxels per agent radius", EditorStyles.miniLabel);
                        }
                        if (m_OverrideVoxelSize.boolValue)
                            EditorGUILayout.HelpBox("Voxel size controls how accurately the navigation mesh is generated from the level geometry. A good voxel size is 2-4 voxels per agent radius. Making voxel size smaller will increase build time.", MessageType.None);
                    }
                    EditorGUI.indentLevel--;
                }

                // Override tile size
                EditorGUILayout.PropertyField(m_OverrideTileSize);

                using (new EditorGUI.DisabledScope(!m_OverrideTileSize.boolValue || m_OverrideTileSize.hasMultipleDifferentValues))
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(m_TileSize);

                    if (!m_TileSize.hasMultipleDifferentValues && !m_VoxelSize.hasMultipleDifferentValues)
                    {
                        float tileWorldSize = m_TileSize.intValue * m_VoxelSize.floatValue;
                        EditorGUILayout.LabelField(" ", tileWorldSize.ToString("0.00") + " world units", EditorStyles.miniLabel);
                    }

                    if (!m_OverrideTileSize.hasMultipleDifferentValues)
                    {
                        if (m_OverrideTileSize.boolValue)
                            EditorGUILayout.HelpBox("Tile size controls the how local the changes to the world are (rebuild or carve). Small tile size allows more local changes, while potentially generating more data overall.", MessageType.None);
                    }
                    EditorGUI.indentLevel--;
                }


                // Height mesh
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(m_BuildHeightMesh);
                }

                EditorGUILayout.Space();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();

            var hadError = false;
            var multipleTargets = targets.Length > 1;
            foreach (NavMeshSurface navSurface in targets)
            {
                var settings = navSurface.GetBuildSettings();
                // Calculating bounds is potentially expensive when unbounded - so here we just use the center/size.
                // It means the validation is not checking vertical voxel limit correctly when the surface is set to something else than "in volume".
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                if (navSurface.collectObjects == CollectObjects.Volume)
                {
                    bounds = new Bounds(navSurface.center, navSurface.size);
                }

                var errors = settings.ValidationReport(bounds);
                if (errors.Length > 0)
                {
                    if (multipleTargets)
                        EditorGUILayout.LabelField(navSurface.name);
                    foreach (var err in errors)
                    {
                        EditorGUILayout.HelpBox(err, MessageType.Warning);
                    }
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.labelWidth);
                    if (GUILayout.Button("Open Agent Settings...", EditorStyles.miniButton))
                        NavMeshEditorHelpers.OpenAgentSettings(navSurface.agentTypeID);
                    GUILayout.EndHorizontal();
                    hadError = true;
                }
            }

            if (hadError)
                EditorGUILayout.Space();

#if NAVMESHCOMPONENTS_SHOW_NAVMESHDATA_REF
            var nmdRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(nmdRect, GUIContent.none, m_NavMeshData);
            var rectLabel = EditorGUI.PrefixLabel(nmdRect, GUIUtility.GetControlID(FocusType.Passive), new GUIContent(m_NavMeshData.displayName));
            EditorGUI.EndProperty();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUI.BeginProperty(nmdRect, GUIContent.none, m_NavMeshData);
                EditorGUI.ObjectField(rectLabel, m_NavMeshData, GUIContent.none);
                EditorGUI.EndProperty();
            }
#endif
            using (new EditorGUI.DisabledScope(Application.isPlaying || m_AgentTypeID.intValue == -1))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);
                if (GUILayout.Button("Clear"))
                {
                    NavMeshAssetManager.instance.ClearSurfaces(targets);
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Bake"))
                {
                    NavMeshAssetManager.instance.StartBakingSurfaces(targets);
                }

                GUILayout.EndHorizontal();
            }

            // Show progress for the selected targets
            var bakeOperations = NavMeshAssetManager.instance.GetBakeOperations();
            for (int i = bakeOperations.Count - 1; i >= 0; --i)
            {
                if (!targets.Contains(bakeOperations[i].surface))
                    continue;

                var oper = bakeOperations[i].bakeOperation;
                if (oper == null)
                    continue;

                var p = oper.progress;
                if (oper.isDone)
                {
                    SceneView.RepaintAll();
                    continue;
                }

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Cancel", EditorStyles.miniButton))
                {
                    var bakeData = bakeOperations[i].bakeData;
                    UnityEngine.AI.NavMeshBuilder.Cancel(bakeData);
                    bakeOperations.RemoveAt(i);
                }

                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), p, "Baking: " + (int)(100 * p) + "%");
                if (p <= 1)
                    Repaint();

                GUILayout.EndHorizontal();
            }
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.Pickable)]
        static void RenderBoxGizmoSelected(NavMeshSurface navSurface, GizmoType gizmoType)
        {
            //navSurface.navMeshDataInstance.FlagAsInSelectionHierarchy();
            var method = navSurface.navMeshDataInstance.GetType().GetMethod("FlagAsInSelectionHierarchy", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(navSurface.navMeshDataInstance, null);
            RenderBoxGizmo(navSurface, gizmoType, true);
        }

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        static void RenderBoxGizmoNotSelected(NavMeshSurface navSurface, GizmoType gizmoType)
        {
                RenderBoxGizmo(navSurface, gizmoType, false);
        }

        static void RenderBoxGizmo(NavMeshSurface navSurface, GizmoType gizmoType, bool selected)
        {
            var color = selected ? s_HandleColorSelected : s_HandleColor;
            if (!navSurface.enabled)
                color = s_HandleColorDisabled;

            var oldColor = Gizmos.color;
            var oldMatrix = Gizmos.matrix;

            // Use the unscaled matrix for the NavMeshSurface
            var localToWorld = Matrix4x4.TRS(navSurface.transform.position, navSurface.transform.rotation, Vector3.one);
            Gizmos.matrix = localToWorld;

            if (navSurface.collectObjects == CollectObjects.Volume)
            {
                Gizmos.color = color;
                Gizmos.DrawWireCube(navSurface.center, navSurface.size);

                if (selected && navSurface.enabled)
                {
                    var colorTrans = new Color(color.r * 0.75f, color.g * 0.75f, color.b * 0.75f, color.a * 0.15f);
                    Gizmos.color = colorTrans;
                    Gizmos.DrawCube(navSurface.center, navSurface.size);
                }
            }
            else
            {
                if (navSurface.navMeshData != null)
                {
                    var bounds = navSurface.navMeshData.sourceBounds;
                    Gizmos.color = Color.grey;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }

            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;

            Gizmos.DrawIcon(navSurface.transform.position, "NavMeshSurface Icon", true);
        }

        void OnSceneGUI()
        {
            if (!editingCollider)
                return;

            var navSurface = (NavMeshSurface)target;
            var color = navSurface.enabled ? s_HandleColor : s_HandleColorDisabled;
            var localToWorld = Matrix4x4.TRS(navSurface.transform.position, navSurface.transform.rotation, Vector3.one);
            using (new Handles.DrawingScope(color, localToWorld))
            {
                m_BoundsHandle.center = navSurface.center;
                m_BoundsHandle.size = navSurface.size;

                EditorGUI.BeginChangeCheck();
                m_BoundsHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(navSurface, "Modified NavMesh Surface");
                    Vector3 center = m_BoundsHandle.center;
                    Vector3 size = m_BoundsHandle.size;
                    navSurface.center = center;
                    navSurface.size = size;
                    EditorUtility.SetDirty(target);
                }
            }
        }

        [MenuItem("GameObject/Navigation/NavMesh Surface", false, 2000)]
        public static void CreateNavMeshSurface(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = NavMeshComponentsGUIUtility.CreateAndSelectGameObject("NavMesh Surface", parent);
            go.AddComponent<NavMeshSurface>();
            var view = SceneView.lastActiveSceneView;
            if (view != null)
                view.MoveToView(go.transform);
        }

        [MenuItem("LayaAir3D/NavMesh/Export2")]
        static void LayaAir3DExportV2()
        {
            Debug.Log("NavMesh Export Start");
            NavMeshTriangulation navMeshTriangulation = NavMesh.CalculateTriangulation();
            if (!Directory.Exists(Application.dataPath + "/ExportNavMesh"))
            {
                Directory.CreateDirectory(Application.dataPath + "/ExportNavMesh");//不存在就创建目录
            }
            //文件路径
            string path = Application.dataPath + "/ExportNavMesh/test.obj";
            //新建文件
            StreamWriter streamWriter = new StreamWriter(path);
            //顶点  
            for (int i = 0; i < navMeshTriangulation.vertices.Length; i++)
            {
                // streamWriter.WriteLine("v  " + (-1 * navMeshTriangulation.vertices[i].x) + " " + navMeshTriangulation.vertices[i].y + " " + navMeshTriangulation.vertices[i].z);
                // streamWriter.WriteLine("v  " + (-1 * navMeshTriangulation.vertices[i].x) + " " + navMeshTriangulation.vertices[i].z + " " + navMeshTriangulation.vertices[i].y);
                streamWriter.WriteLine("v  " + (-1 * navMeshTriangulation.vertices[i].x) + " " + 0 + " " + navMeshTriangulation.vertices[i].y);
            }
            streamWriter.WriteLine("g pPlane1");
            //索引  
            for (int i = 0; i < navMeshTriangulation.indices.Length;)
            {
                streamWriter.WriteLine("f " + (navMeshTriangulation.indices[i] + 1) + " " + (navMeshTriangulation.indices[i + 1] + 2) + " " + (navMeshTriangulation.indices[i + 1] + 1));
                i = i + 3;
            }
            streamWriter.Flush();
            streamWriter.Close();
            AssetDatabase.Refresh();
            Debug.Log("NavMesh Export Success");
        }
[MenuItem("LayaAir3D/NavMesh/Export3")]
    static void LayaAir3DExport()
    {
        UnityEngine.Debug.Log("CreatePolyNavMesh Start");
        CreatePolyNavMesh();
        UnityEngine.Debug.Log("CreatePolyNavMesh End");
    }
          private static void CreatePolyNavMesh()
        {
            UnityEngine.AI.NavMeshTriangulation triangulatedWalkNavMesh = Path1();
            string path = System.Environment.CurrentDirectory.Replace("\\", "/") + "/Config/NavMeshBuild/";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            float startX = 0f;
            float startZ = 0f;
            float endX = 500f;
            float endZ = 500f;
            
            StringBuilder sb = new StringBuilder("{");
            sb.Append("\"mapID\":").Append(001);
            sb.Append(",\"startX\":").Append(startX).Append(",\"startZ\":").Append(startZ);
            sb.Append(",\"endX\":").Append(endX).Append(",\"endZ\":").Append(endZ);
            string filename = path +  "001.navmesh";

            string data = "";
            data = PathMeshToString(triangulatedWalkNavMesh);
            sb.Append(",").Append(data);
            sb.Append("}");
            MeshToFile(filename, sb.ToString());
        }
        
        /// <summary>
        /// 计算行走层三角网格
        /// </summary>
        /// <returns></returns>
        private static UnityEngine.AI.NavMeshTriangulation Path1()
        {
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            UnityEngine.AI.NavMeshTriangulation triangulatedNavMesh = UnityEngine.AI.NavMesh.CalculateTriangulation();
            return triangulatedNavMesh;
        }
        
        /// <summary>
        /// 寻路数据转换为字符串
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        private static string PathMeshToString(UnityEngine.AI.NavMeshTriangulation mesh)
        {
            if (mesh.indices.Length < 1)
            {
                return "";
            }

            float scalar = 1f;
            float offsetX = 51f;
            float offsetY = 38f;
            StringBuilder sb = new StringBuilder();
            sb.Append( "\"pathTriangles\":[");
            foreach (var t in mesh.indices)
            {
                sb.Append(t).Append(",");
            }
            sb.Length--;
            sb.Append("],");

            sb.Append("\"pathVertices\":[");
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                Vector3 v = mesh.vertices[i];
                var vX = v.x+offsetX;
                var vY = 0f;
                var vZ = v.y+offsetY;
                
                vX *= scalar;
                vY *= scalar;
                vZ *= scalar;
                
                sb.Append("{\"x\":").Append(vX).Append(",\"y\":").Append(vY).Append(",\"z\":").Append(vZ).Append("},");
                // sb.Append("{\"x\":").Append(v.x).Append(",\"y\":").Append(v.y).Append(",\"z\":").Append(v.z).Append("},");
            }
            sb.Length--;
            sb.Append("]");
            return sb.ToString();
        }
        
        static void MeshToFile(string filename, string meshData)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.Write(meshData);
            }
        }
    }
}
