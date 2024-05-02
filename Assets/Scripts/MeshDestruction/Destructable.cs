using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destructable : MonoBehaviour
{
    [SerializeField] private float force;
    [SerializeField] private int numPieces;
    [SerializeField] private int numGenerations;

    private void OnCollisionEnter(Collision other)
    {
        //RaycastHit hit;
        //if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
        if(other.gameObject.CompareTag("Ball"))
        {
            //Debug.Log(hit.point);//world space point
            //Debug.Log(transform.worldToLocalMatrix * hit.point);//local point

            var objs = MeshDestruction.MeshDestruction.DestroyMesh(transform, other.transform.position, numPieces);

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
                rb.AddForce((obj.GetComponent<MeshFilter>().mesh.bounds.center - other.transform.position) * force, ForceMode.Impulse);
            }
            Destroy(gameObject);
        }
        else
        {
            //Debug.Log("Didn't find Collision?");
        }
    }
}
