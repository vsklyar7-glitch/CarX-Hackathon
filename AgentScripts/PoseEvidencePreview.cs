using System.Linq;
using UnityEngine;
public static class PoseEvidencePreview
{
    public static string Wide()
    {
        var item=Object.FindObjectsByType<EvidenceItem>().Single(i=>i.evidenceId=="family_photo");
        var player=Object.FindAnyObjectByType<PlayerInteraction>();player.GetComponent<PlayerController>().SetControlsBlocked(true);
        player.playerCamera.transform.position=item.transform.position+new Vector3(.12f,.78f,-.9f);
        player.playerCamera.transform.LookAt(item.transform.position+new Vector3(0,.03f,.30f));
        return "Apartment eye-height preview";
    }
    public static string Inspect()
    {
        var player=Object.FindAnyObjectByType<PlayerInteraction>();player.GetComponent<PlayerController>().SetControlsBlocked(false);
        player.Inspect(Object.FindObjectsByType<EvidenceItem>().Single(i=>i.evidenceId=="family_photo"));
        return "Inspection feedback preview";
    }
}
