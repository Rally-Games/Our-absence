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
            LeftAttack();
        }
        if (playerRightAttack.triggered)
        {
            RightAttack();
        }
    }

    private void LeftAttack()
    {
        if (player_controller.GetComponent<Animator>().GetLayerWeight(1) == 1)
        {
            player_controller.ChangeAnimation("Boxing left lock on", 0.05f);
        }
        else
        {
            player_controller.ChangeAnimation("Boxing left", 0.05f);

        }
    }

    private void RightAttack()
    {
        if (player_controller.GetComponent<Animator>().GetLayerWeight(1) == 1)
        {
            player_controller.ChangeAnimation("Boxing right lock on", 0.05f);
        }
        else
        {
            player_controller.ChangeAnimation("Boxing right", 0.05f);
        }
    }


}
