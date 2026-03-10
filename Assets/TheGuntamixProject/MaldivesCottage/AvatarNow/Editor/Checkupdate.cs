using System.IO;
using UnityEditor;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Linq;

namespace AvatarNowEditor
{
    [InitializeOnLoad]
    public class EditorStartup : MonoBehaviour
    {
        private static string TMP_FILE_PATH = "Temp/AvatarNowVersionCheck";
        private static string VER = "1.04";

        static EditorStartup()
        {
            if (File.Exists(TMP_FILE_PATH))
            {
            }
            else
            {
                File.Create(TMP_FILE_PATH);
                EditorCoroutine.Start(GetText());

                string packageName = "net.koyashiro.genericdatacontainer"; // チェックしたいパッケージ名
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/" + packageName);
                if(packageInfo==null)
                {
                    if(EditorUtility.DisplayDialog("AvatarNow",packageName + " GenericDataContainerがインストールされていません。\nVCCでGenericDataContainerをインストールしてください","手順を開く","後で"))
                        Application.OpenURL("https://docs.google.com/document/d/1A2hOq60U2gvYb_25NanGXzjz5GfblxK79kTEDAjcBtQ/edit?usp=sharing");
                }
            }
        }

        public static string requestURL = "https://gist.github.com/wolf2064/a279f2b3c8fdea2150006c553cdc162e/raw";

        static IEnumerator GetText()
        {

            UnityWebRequest www = UnityWebRequest.Get(requestURL);
            yield return www.SendWebRequest();

            if (www.result==UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
                if (www.downloadHandler.text != VER)
                {
                    if (EditorUtility.DisplayDialog("AvatarNow", "更新があります！\nBoothからダウンロードしてください", "Boothのページを開く", "後で更新する"))
                        Application.OpenURL("https://w01fa.booth.pm/items/2525069");
                }
                else
                {
                    Debug.Log("AvatarNow ver" + VER + "は最新版です");
                }
            }
        }
    }
    public sealed class EditorCoroutine
    {
        static EditorCoroutine()
        {
            EditorApplication.update += Update;
            //Debug.Log("EditorCoroutine SetUp");
        }

        static Dictionary<IEnumerator, EditorCoroutine.Coroutine> asyncList = new Dictionary<IEnumerator, Coroutine>();
        static List<EditorCoroutine.WaitForSeconds> waitForSecondsList = new List<EditorCoroutine.WaitForSeconds>();

        static void Update()
        {

            CheackIEnumerator();
            CheackWaitForSeconds();
        }

        static void CheackIEnumerator()
        {
            List<IEnumerator> removeList = new List<IEnumerator>();
            foreach (KeyValuePair<IEnumerator, EditorCoroutine.Coroutine> pair in asyncList)
            {
                if (pair.Key != null)
                {

                    //IEnumratorのCurrentがCoroutineを返しているかどうか 
                    EditorCoroutine.Coroutine c = pair.Key.Current as EditorCoroutine.Coroutine;
                    if (c != null)
                    {
                        if (c.isActive) continue;
                    }
                    //これ以上MoveNextできなければ終了 
                    if (!pair.Key.MoveNext())
                    {
                        if (pair.Value != null)
                        {
                            pair.Value.isActive = false;
                        }
                        removeList.Add(pair.Key);
                    }
                }
                else
                {
                    removeList.Add(pair.Key);
                }
            }

            foreach (IEnumerator async in removeList)
            {
                asyncList.Remove(async);
            }
        }

        static void CheackWaitForSeconds()
        {
            for (int i = 0; i < waitForSecondsList.Count; i++)
            {
                if (waitForSecondsList[i] != null)
                {
                    if (EditorApplication.timeSinceStartup - waitForSecondsList[i].InitTime > waitForSecondsList[i].Time)
                    {
                        waitForSecondsList[i].isActive = false;
                        waitForSecondsList.RemoveAt(i);
                    }
                }
                else
                {
                    Debug.LogError("rem");
                    waitForSecondsList.RemoveAt(i);
                }
            }
        }
        static public EditorCoroutine.Coroutine Start(IEnumerator iEnumerator)
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                EditorCoroutine.Coroutine c = new Coroutine();
                if (!asyncList.Keys.Contains(iEnumerator)) asyncList.Add(iEnumerator, c);
                iEnumerator.MoveNext();
                return c;
            }
            else
            {
                Debug.LogError("EditorCoroutine.Startはゲーム起動中に使うことはできません");
                return null;
            }
        }
        static public void Stop(IEnumerator iEnumerator)
        {
            if (Application.isEditor)
            {
                if (asyncList.Keys.Contains(iEnumerator))
                {
                    asyncList.Remove(iEnumerator);
                }
            }
            else
            {
                Debug.LogError("EditorCoroutine.Startはゲーム中に使うことはできません");
            }
        }
        static public void AddWaitForSecondsList(EditorCoroutine.WaitForSeconds coroutine)
        {
            if (waitForSecondsList.Contains(coroutine) == false)
            {
                waitForSecondsList.Add(coroutine);
            }
        }

        public class Coroutine
        {
            public bool isActive;

            public Coroutine()
            {
                isActive = true;
            }
        }

        public sealed class WaitForSeconds : EditorCoroutine.Coroutine
        {
            private float time;
            private double initTime;

            public float Time
            {
                get { return time; }
            }
            public double InitTime
            {
                get { return initTime; }
            }

            public WaitForSeconds(float time) : base()
            {
                this.time = time;
                this.initTime = EditorApplication.timeSinceStartup;
                EditorCoroutine.AddWaitForSecondsList(this);
            }
        }
    }
}