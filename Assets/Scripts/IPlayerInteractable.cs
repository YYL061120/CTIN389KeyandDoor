using UnityEngine;

/// <summary>Shared contract for proximity-based E interactions.</summary>
public interface IPlayerInteractable
{
    string InteractionPrompt { get; }
    float InteractionDistance { get; }
    bool CanInteract(PlayerInteractionContext context);
    Vector3 GetInteractionPoint(Vector3 playerPosition);
    void Interact(PlayerInteractionContext context);
}

public sealed class PlayerInteractionContext
{
    public readonly GameObject Player;
    public readonly PlayerInventory Inventory;
    public readonly FirstPersonController Controller;
    public readonly Camera Camera;
    public readonly PlayerInteractionController InteractionController;

    public PlayerInteractionContext(GameObject player, PlayerInventory inventory,
        FirstPersonController controller, Camera camera,
        PlayerInteractionController interactionController)
    {
        Player = player;
        Inventory = inventory;
        Controller = controller;
        Camera = camera;
        InteractionController = interactionController;
    }

    public void ShowMessage(string message, float duration = 2f)
    {
        InteractionController.ShowMessage(message, duration);
    }
}
