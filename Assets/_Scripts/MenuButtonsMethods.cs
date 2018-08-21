using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Ardunity;
using Valve.VR;

namespace Valve.VR.InteractionSystem
{
    public class MenuButtonsMethods : MonoBehaviour
    {
        public GameObject[] gameObjects;
        public Transform[] spawnPoints;
        private Coroutine resetSceneCoroutine;

        public void ResetSceneOnClick()
        {
            if (resetSceneCoroutine != null)
            {
                StopCoroutine(resetSceneCoroutine);
            }
            resetSceneCoroutine = StartCoroutine(RespawnObjects());
        }

        private IEnumerator RespawnObjects()
        {
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (gameObjects[i] && spawnPoints[i])
                {
                    gameObjects[i].GetComponent<Rigidbody>().velocity = Vector3.zero;
                    gameObjects[i].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                    gameObjects[i].GetComponent<Transform>().position = spawnPoints[i].GetComponent<Transform>().position;
                    gameObjects[i].GetComponent<Transform>().rotation = Quaternion.identity;
                }
            }
           yield return new WaitForSeconds(1);
        }
    }
}
