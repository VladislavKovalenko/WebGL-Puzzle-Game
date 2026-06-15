using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace TestEditorTools
{
    public static class MultipleSceneValidator
    {
        static ILogger Logger => Debug.unityLogger;

        [MenuItem("Tools/Megxlord/Test/tmp IterateScenes")]
        public static void IterateScenes()
        {
            var scenePaths = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath); //точно определяем папку в которой будем искать
            foreach (var scenePath in scenePaths)
            {
                Logger.Log(scenePath);
            }
            
        }
        
        

        [MenuItem("Tools/Megxlord Hierarchy/MultipleSceneMissingComponents😶‍🌫️")]
        public static void FindMissingComponents()
        { 
            var scene = SceneManager.GetActiveScene();
            //Итерироваться по сцене лучше через рекурсию
            //Но использовать буду хвостовую рекурсию, рекурсия выражена через очередь

            var gameObjectsQueue = new Queue<GameObject>(scene.GetRootGameObjects());

            while (gameObjectsQueue.Count > 0)
            {
                var gameObject = gameObjectsQueue.Dequeue();
                FindMissingComponents(gameObject);
                
                foreach (Transform child in gameObject.transform)
                    gameObjectsQueue.Enqueue(child.gameObject);
            }
        }

        public static void FindMissingComponents(GameObject gameObject)
        {
            //Logger.Log(gameObject.name); //тут просто выводит список всех объектов со сцены
            var hasMissingScript = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject) > 0;

            if (hasMissingScript)
            {
                Logger.LogWarning("1",$"GameObject {gameObject.name} has missing script");
            }
        }
    }
}