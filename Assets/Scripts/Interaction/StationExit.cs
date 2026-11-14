using UnityEngine;

// Зона подхода находится только со стороны используемой комнаты участка.
[RequireComponent(typeof(BoxCollider))]
public class StationExit : MonoBehaviour, IInteractable
{
    public Transform apartmentSpawn;
    public string Prompt => "E - покинуть участок";

    public bool IsNearby(PlayerInteraction player)
    {
        if (player == null || apartmentSpawn == null) return false;
        BoxCollider area = GetComponent<BoxCollider>();
        Vector3 point = player.transform.position + Vector3.up * 0.9f;
        return area.enabled && (area.ClosestPoint(point) - point).sqrMagnitude < 0.0001f;
    }

    public void Interact(PlayerInteraction player)
    {
        if (!IsNearby(player)) return;
        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller.ControlsBlocked) return;
        controller.Teleport(apartmentSpawn);
        if (player.inspectionUI != null) player.inspectionUI.SetPrompt("");
    }
}
