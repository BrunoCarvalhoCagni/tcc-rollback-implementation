using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CatControl : MonoBehaviour {

	float dirX, moveSpeed;

	Animator anim;

	// Use this for initialization
	void Start () {
		anim = GetComponent<Animator> ();
		moveSpeed = 5f;
	}
	
	// Update is called once per frame
	void Update () {
		dirX = Input.GetAxisRaw ("Horizontal") * moveSpeed * Time.deltaTime;

		transform.position = new Vector2 (transform.position.x + dirX, transform.position.y);

		if (dirX != 0 && !anim.GetCurrentAnimatorStateInfo(0).IsName("LowKick") && !anim.GetCurrentAnimatorStateInfo(0).IsName("TwoSide") && !anim.GetCurrentAnimatorStateInfo(0).IsName("OneTwo")) {
			anim.SetBool ("isWalking", true);
		}
		else {
			anim.SetBool ("isWalking", false);
		}
		if(!anim.GetCurrentAnimatorStateInfo(0).IsName("LowKick") && !anim.GetCurrentAnimatorStateInfo(0).IsName("OneTwo") && !anim.GetCurrentAnimatorStateInfo(0).IsName("TwoSide")){
		if (Input.GetKeyDown ("u")) {
			anim.SetBool ("isWalking", false);
			anim.SetTrigger ("hitLowKick");
		}
		else if (Input.GetKeyDown ("i")) {
			anim.SetBool ("isWalking", false);
			anim.SetTrigger ("hitOneTwo");
		}
		else if (Input.GetKeyDown ("o")) {
			anim.SetBool ("isWalking", false);
			anim.SetTrigger ("hitTwoSide");	
		}
		}
		else if(Input.GetKeyDown ("i") && anim.GetCurrentAnimatorStateInfo(0).IsName("LowKick") && anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.3f &&anim.GetCurrentAnimatorStateInfo(0).normalizedTime <= 0.75f){
		anim.SetBool ("isWalking", false);
		anim.SetTrigger ("hitOneTwo");
		

		}
		


		if(!anim.GetCurrentAnimatorStateInfo(0).IsName("Walk") && !anim.GetCurrentAnimatorStateInfo(0).IsName("Idle")){
			moveSpeed = 0f;

		}else{
			moveSpeed = 5f;

		}

	}
}
