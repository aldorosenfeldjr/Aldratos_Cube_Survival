using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Hazard : MonoBehaviour
{
    Vector3 rotation;

    [SerializeField] 
    private ParticleSystem breakingEffect;
    private Player player;

    private void Start()
    {
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
        if (!collision.gameObject.CompareTag("Hazard") && !collision.gameObject.CompareTag("PowerUp"))
            {
                Destroy(gameObject);
                Instantiate(breakingEffect, transform.position, Quaternion.identity);
                AudioManager.Play(Sfx.Land, player != null ? Mathf.Clamp01(1f / Mathf.Max(Vector3.Distance(transform.position, player.transform.position), 1f) + 0.3f) : 0.5f);

                if (player != null && CameraShaker.Instance != null)
                {
                    var distance = Vector3.Distance(transform.position, player.transform.position);
                    var force = 1f / Mathf.Max(distance, 1f);

                    CameraShaker.Instance.ShakeImpact(force);
                }
            }
    }
}
