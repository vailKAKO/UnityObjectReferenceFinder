using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ReferenceFinder : EditorWindow
{
    private string _targetComponentName;
    private List<GameObject> _foundAssets = new List<GameObject>();
    private Vector2 _currentScrollPosition;
    private static IEnumerable<Type> _cashedComponents;
    private static Dictionary<string, List<Type>> _typeDict;

    static MonoScript[] _monoScripts;

    private static bool _isFirstTime = true;

    //開かれ方と、開かれたときの挙動
    [MenuItem("ReferenceFinder/Search")]
    private static void Open()
    {
        //開かれる際には一応取得
        _cashedComponents = GetAllTypes();
        GetWindow<ReferenceFinder>("Components Reference Finder.");
    }

    private void OnGUI()
    {
        //Editorに枠を出して、入力を_targetComponentNameに格納する
        EditorGUILayout.BeginHorizontal();
        _targetComponentName =
            EditorGUILayout.TextField("Target Component Name: ", _targetComponentName);
        EditorGUILayout.EndHorizontal();

        if (_foundAssets.Count > 0)
        {
            _currentScrollPosition = EditorGUILayout.BeginScrollView(_currentScrollPosition);
            foreach (var asset in _foundAssets)
            {
                EditorGUILayout.ObjectField(asset.name, asset, typeof(GameObject), false);
            }

            EditorGUILayout.EndScrollView();
        }

        if (GUILayout.Button("Search"))
        {
            _foundAssets.Clear();

            var guids = AssetDatabase.FindAssets("t:GameObject", null);


            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var loadAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var typeCash = GetType(_targetComponentName) ?? null;

                if (typeCash == null)
                {
                    Debug.Log("No Type Found.");
                    return;
                }

                var tmp = loadAsset.GetComponentsInChildren(GetType(_targetComponentName) ?? null);

                foreach (var kari in tmp)
                {
                    _foundAssets.Add(kari.gameObject);
                }
            }
        }
    }


    /// <summary>
    /// プロジェクト内に存在する全スクリプトファイル
    /// </summary>
    static MonoScript[] MonoScripts
    {
        get { return _monoScripts ?? (_monoScripts = Resources.FindObjectsOfTypeAll<MonoScript>().ToArray()); }
    }

    /// <summary>
    /// クラス名からタイプを取得する
    /// </summary>
    private static Type GetType(string className)
    {
        if (_typeDict == null)
        {
            // Dictionary作成
            _typeDict = new Dictionary<string, List<Type>>();
            foreach (var type in _cashedComponents)
            {
                if (!_typeDict.ContainsKey(type.Name))
                {
                    _typeDict.Add(type.Name, new List<Type>());
                }

                _typeDict[type.Name].Add(type);
            }
        }

        //クラスが存在する場合、リストに表示
        if (_typeDict.ContainsKey(className))
        {
            return _typeDict[className][0];
        }
        else
        {
            //クラスが存在しない場合、念の為取得、再走
            if (_isFirstTime)
            {
                Debug.Log("Not found. ReScanning...");
                _cashedComponents = GetAllTypes();
                _isFirstTime = false;
                GetType(className);
            }

            _isFirstTime = true;

            return null;
        }
    }

    /// <summary>
    /// 全てのクラスタイプを取得
    /// </summary>
    private static IEnumerable<Type> GetAllTypes()
    {
        //Unity標準のクラスタイプを取得する
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(asm => asm.GetTypes())
            .Where(type => type != null && !string.IsNullOrEmpty(type.Namespace))
            .Where(type => type.Namespace.Contains("UnityEngine"));

        //自作クラスも取得できるように
        var localTypes = MonoScripts
            .Where(script => script != null)
            .Select(script => script.GetClass())
            .Where(classType => classType != null)
            .Where(classType => classType.Module.Name == "Assembly-CSharp.dll");

        return types.Concat(localTypes).Distinct();
    }
}