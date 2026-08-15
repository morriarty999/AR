using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Deplacable_SpringJoint : MonoBehaviour
{
    Rigidbody rb;
    SpringJoint joint;
    Transform holdAnchor; // ancre unique créée à la prise

    [Header("Réglages du ressort")]
    public float forceRessort = 500f;
    public float amortissement = 60f;
    public float distanceRepos = 0f;
    public float masseRelative = 1f;

    // options
    public bool freezeRotationWhileHeld = true;
    RigidbodyConstraints prevConstraints;
    float prevAngularDrag;
    float prevDrag;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        // si ta version a CollisionDetection, mets Continuous Dynamic si dispo
        // rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    // Prendre : on fournit une ancre (transform) unique pour cet objet
    public void Prendre(Transform anchor)
    {
        // retire joint existant si besoin
        if (joint != null) Destroy(joint);

        holdAnchor = anchor;

        // sauvegarde état
        prevConstraints = rb.constraints;
        prevAngularDrag = rb.angularDrag;
        prevDrag = rb.drag;

        // crée le joint
        joint = gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = null;
        joint.connectedAnchor = holdAnchor.position;
        joint.spring = forceRessort;
        joint.damper = amortissement;
        joint.minDistance = distanceRepos;
        joint.maxDistance = distanceRepos;
        joint.massScale = masseRelative;

        // comportement pendant prise
        rb.useGravity = false;
        rb.drag = 3f;
        rb.angularDrag = 6f;

        if (freezeRotationWhileHeld)
            rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    // Lâcher
    public void Lacher()
    {
        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }

        // si on a une ancre temporaire créée par l'input manager, on ne la détruit pas ici
        // (l'input manager est responsable de la détruire après appel Lacher)

        // restaurer physique
        rb.useGravity = true;
        rb.drag = prevDrag;
        rb.angularDrag = prevAngularDrag;
        rb.constraints = prevConstraints;

        holdAnchor = null;
    }

    void FixedUpdate()
    {
        // Met à jour l'ancre du joint à la position courante de l'anchor fournie
        if (joint != null && holdAnchor != null)
        {
            joint.connectedAnchor = holdAnchor.position;
        }
    }
}
