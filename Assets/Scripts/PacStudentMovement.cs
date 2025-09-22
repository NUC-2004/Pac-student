using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacStudentMovement : MonoBehaviour
{
    public float speed = 2f;   
    private int currentIndex = 0;

    private Vector3[] waypoints = new Vector3[]
    {
        new Vector3(-6.04f, 13.8f, 0f), 
        new Vector3( -0.91f, 13.93f, 0f),  
         new Vector3(-0.91f, 9.87f, 0f), 
        new Vector3( -5.91f, 9.87f, 0f)
         
    };

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        Vector3 target = waypoints[currentIndex];

        float dx = target.x - transform.position.x;
        float dy = target.y - transform.position.y;

        float distance = Mathf.Sqrt(dx * dx + dy * dy);

        if (distance > 0.01f)
        {
            float dirX = dx / distance;
            float dirY = dy / distance;

            transform.position += new Vector3(dirX * speed * Time.deltaTime,dirY * speed * Time.deltaTime, 0f);

            if (Mathf.Abs(dirX) > Mathf.Abs(dirY))
            {
                if (dirX > 0) animator.Play("PlayerRight");
                else animator.Play("PlayerLeft");
            }
            else
            {
                if (dirY > 0) animator.Play("PlayerUp");
                else animator.Play("PlayerDown");
            }
        }
        else
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
        }
    }
}
