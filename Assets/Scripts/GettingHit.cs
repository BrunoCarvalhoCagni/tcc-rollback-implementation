using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace tcc{
public class GettingHit : MonoBehaviour
{
    Animator anim;

    void Start(){
        anim = GetComponent<Animator> ();
    }

    void OnTriggerEnter2D(Collider2D col){
        if(col.gameObject.name.Equals ("Cat")){
            anim.SetTrigger ("gotHit");
            transform.position = new Vector2 (transform.position.x + 0.3f, transform.position.y);
        }
    }
}
}
