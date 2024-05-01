using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject _ballPrefab;
    
    public void SpawnBall()
    {
        Instantiate(_ballPrefab);
    }
}
