using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandAnimator : MonoBehaviour
{

    public Animator Animator;
    public InputActionReference Grip;
    private InputAction Grip_Action;
    public InputActionReference Trigger;
    public InputAction Trigger_Action;
    // Start is called before the first frame update
    void Start()
    {
        Grip_Action = Grip.action;
        Trigger_Action = Trigger.action;
    }

    // Update is called once per frame
    void Update()
    {
        Animator.SetFloat("Grip", Grip_Action.ReadValue<float>());
        Animator.SetFloat("Trigger", Trigger_Action.ReadValue<float>());
    }
}
