using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Hazard : MonoBehaviour
{
    Vector3 rotation;

    [SerializeField] 
    private ParticleSystem breakingEffect;
    private Unity.Cinemachine.CinemachineImpulseSource cinemachineImpulseSource;
    private Player player;
    
    private void Start() 
    {
        cinemachineImpulseSource = GetComponent<Unity.Cinemachine.CinemachineImpulseSource>();
        player = FindAnyObjectByType<Player>();

        var xRotation = Random.Range(90f, 180f);
        rotation = new Vector3(-xRotation, 0);
    }
    
    private void Update() 
    {
        transform.Rotate(rotation * Time.deltaTime);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Hazard"))
            {
                Destroy(gameObject);
                Instantiate(breakingEffect, transform.position, Quaternion.identity);

                if (player != null)
                {
                    var distance = Vector3.Distance(transform.position, player.transform.position);
                    var force = 1f / Mathf.Max(distance, 1f);

                    cinemachineImpulseSource.GenerateImpulse(force);
                }
            }
    }
}
