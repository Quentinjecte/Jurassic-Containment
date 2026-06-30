using Demo.Scripts.Runtime.Item;
//using Unity.Entities;
using Unity.Mathematics;
//using Unity.Rendering;
//using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

public class Bullet : MonoBehaviour
{
    /*public Weapon _Weapon;
    [SerializeField] private GameObject hitEffect;

    public float Range = 2000f;

    [SerializeField] private Transform Cannon;   // Sortie du canon
    [SerializeField] private Transform Chamber;  // Chambre d'éjection
    public BulletData BulletData; // Données de référence

    private EntityManager entityManager;
    private Entity entityPrefab;

    private Entity Owner;

    private void Start()
    {
        _Weapon = GetComponent<Weapon>();
        _Weapon.onFireEvent += Shoot;
        //entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        //CreateEntityPrefab(); // Crée une seule fois le prefab ECS
    }

    private void CreateEntityPrefab()
    {
        MeshFilter meshFilter = BulletData.Head_Ammo.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = BulletData.Head_Ammo.GetComponent<MeshRenderer>();

        entityPrefab = entityManager.CreateEntity(
            ComponentType.ReadWrite<Prefab>(),
            ComponentType.ReadWrite<LocalTransform>(),
            ComponentType.ReadWrite<RenderBounds>(),
            ComponentType.ReadWrite<Unity.Physics.PhysicsCollider>()
        );

        var renderMeshArray = new RenderMeshArray(meshRenderer.sharedMaterials, new[] { meshFilter.sharedMesh });
        var renderMeshDescription = new RenderMeshDescription(
            shadowCastingMode: ShadowCastingMode.Off,
            receiveShadows: false
        );

        RenderMeshUtility.AddComponents(
            entityPrefab,
            entityManager,
            renderMeshDescription,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0)
        );

        var mat = new Unity.Physics.Material
        {
            Friction = 0.5f,
            Restitution = 0,
            FrictionCombinePolicy = Unity.Physics.Material.CombinePolicy.GeometricMean,
            RestitutionCombinePolicy = Unity.Physics.Material.CombinePolicy.GeometricMean,
            CollisionResponse = Unity.Physics.CollisionResponsePolicy.RaiseTriggerEvents,
        };

        // Ajouter un collider
        var collider = Unity.Physics.BoxCollider.Create(
            new Unity.Physics.BoxGeometry
            {
                Center = float3.zero,
                Size = new float3(0.2f),
                Orientation = quaternion.identity,
                BevelRadius = 0.01f
            },
            new Unity.Physics.CollisionFilter
            {
                BelongsTo = 1 << 0,
                CollidesWith = ~1u,
                GroupIndex = 0,
            },
            mat
        );

        entityManager.SetComponentData(entityPrefab, new Unity.Physics.PhysicsCollider
        {
            Value = collider,
            
        });

        entityManager.AddComponentData(entityPrefab, new BulletStatComponent
        {
            Speed = BulletData.Speed / 10,
            Mass = BulletData.Mass,
            Drag = BulletData.DragCoef,
            Lifetime = BulletData.Lifetime,
            Damage = 0,
            Direction = Vector3.zero,
            StartPosition = Vector3.zero,
        });

        entityManager.SetName(entityPrefab,$"bullet : {BulletData.Type}");

        entityManager.AddComponent<Unity.Physics.PhysicsWorldIndex>(entityPrefab);
    }
    
    public void Initialize() // Un call Event lors du mode fire de l'arme.
    {
        return;

        if (entityPrefab == Entity.Null) return;

        Entity bulletInstance = entityManager.Instantiate(entityPrefab);

        // CORRIGÉ — écrire sur l'INSTANCE, pas le prefab
        entityManager.SetComponentData(bulletInstance, new LocalTransform
        {
            Position = Cannon.position,
            Rotation = Cannon.rotation,
            Scale = 1f
        });

        // CORRIGÉ — écrire sur l'instance
        var stats = entityManager.GetComponentData<BulletStatComponent>(bulletInstance);
        stats.Damage = Mathf.CeilToInt(BulletData.Damage);
        stats.DamageCategory = BulletData.DamageCategory; // à ajouter dans BulletData
        stats.Owner = transform.root.GetComponent<CharacterToECSBridge>().PlayerEntity;
        stats.PreviousPosition = Cannon.position; // pour le raycast
        entityManager.SetComponentData(bulletInstance, stats); // ← instance
    }

    public void Shoot(Transform t)
    {
        //var go = Instantiate(BulletData.Head_Ammo, Cannon);
        //go.GetComponent<BulletImpact>().Init(BulletData.Speed);

        //go.transform.parent = null;
        //go.transform.localScale = Vector3.one;

        Vector3 origin = Camera.main.transform.position;
        Vector3 direction = Camera.main.transform.forward;

        if (Physics.Raycast(origin, direction, out UnityEngine.RaycastHit hit, Range))
        {
            if (hit.collider.TryGetComponent<HitboxLink>(out var hitbox))
            {
                Debug.Log(hitbox.Zone);
                var bridge = transform.root.GetComponent<CharacterToECSBridge>();

                bridge.DealDamageToEntity(
                    hitbox.OwnerEntity,
                    BulletData.Damage,
                    BulletData.DamageCategory,
                    hitbox.Zone
                );
                bridge.DealImpactToEntity(
                    hitbox.OwnerEntity,
                    BulletData.Damage,
                    BulletData.DamageCategory,
                    hitbox.Zone,
                    hit.point,
                    direction
                );

                var particle = Instantiate(hitEffect, hit.point, Quaternion.identity);

                foreach (ParticleSystem _particle in particle.GetComponentsInChildren<ParticleSystem>())
                {
                    _particle.Play();
                }
                Destroy(particle, 1);
            }
        }
        // Spawn visuel
        //Vector3 _trailorigin = Cannon.position;
        //Vector3 _traildirection = Cannon.forward;
        //BulletPool.Spawn(_trailorigin, _traildirection);
    }*/
}
