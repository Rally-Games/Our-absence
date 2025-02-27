using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]
public class AttackSystem : MonoBehaviour
{
    private PlayerInput playerInput;
    private Animator animator;
    private InputAction playerLeftAttack;
    private InputAction playerRightAttack;
    private Player_controller player_controller;

    void Start()
    {
        playerInput = GetComponent<PlayerInput>();

        playerLeftAttack = playerInput.actions["Fire"];
        playerRightAttack = playerInput.actions["SecFire"];

        animator = GetComponent<Animator>();

        player_controller = GetComponent<Player_controller>();
    }

    void Update()
    {
        if (player_controller.currentAnimation == "Roll"
        || player_controller.currentAnimation == "Standing Dodge Backward") return;

        if (playerLeftAttack.triggered)
        {
            print("Left Attack");
            LeftAttack();
        }
        if (playerRightAttack.triggered)
        {
            RightAttack();
        }
    }

    private void LeftAttack()
    {
        player_controller.ChangeAnimation("Boxing left", 0.05f);
    }

    private void RightAttack()
    {
        player_controller.ChangeAnimation("Boxing right", 0.05f);
    }


}
