using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructable : MonoBehaviour
{
    [SerializeField] private float force;
    [SerializeField] private int numPieces;
    [SerializeField] private int numGenerations;

    private void OnMouseDown()
    {
        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
        {
            //Debug.Log(hit.point);//world space point
            //Debug.Log(transform.worldToLocalMatrix * hit.point);//local point

            var objs = MeshDestruction.MeshDestruction.DestroyMesh(transform, hit.point, numPieces);

            foreach (var obj in objs)
            {
                if (numGenerations > 0)
                {
                    var dest = obj.AddComponent<Destructable>();
                    dest.force = force;
                    dest.numPieces = numPieces;
                    dest.numGenerations = numGenerations - 1;
                }

                var rb = obj.AddComponent<Rigidbody>();
                rb.velocity = (obj.transform.position - transform.position).normalized * force;
                //Instantiate(obj);
            }
            Destroy(gameObject);
            //var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //obj.transform.position = hit.point;
        }
        else
        {
            Debug.Log("Didn't find Collision?");
        }
    }
}
