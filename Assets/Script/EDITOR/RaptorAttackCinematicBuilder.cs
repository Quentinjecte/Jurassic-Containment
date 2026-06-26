#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;
/// <summary>
/// Génère automatiquement le rig Cinemachine (CM3) pour la cinématique Raptor Attack.
/// Menu : Jurassic Containment → Cinématique → Créer Raptor Attack
/// </summary>
public static class RaptorAttackCinematicBuilder
{
    private const string RootName = "CinematicDirector";
    private const string PachyPrefabPath =
        "Assets/Eternal Asset/FerociousIndustries/PBRDinosaurs/PBRPachycephalasaurus/Prefabs/24K/Pachycephalasaurus_24K.prefab";
    private const string RaptorPrefabPath =
        "Assets/Eternal Asset/FerociousIndustries/PBRDinosaurs/PBRVelociraptor/Prefabs/PBR/LODG/Raptor_Animated_LODG_Orange.prefab";

    [MenuItem("Jurassic Containment/Cinématique/Créer Raptor Attack")]
    public static void CreateRaptorAttackCinematic()
    {
        Transform anchor = Selection.activeTransform;
        Vector3 origin = anchor != null ? anchor.position : FindSceneCenter();

        if (GameObject.Find(RootName) != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "Cinématique existante",
                    $"Un objet « {RootName} » existe déjà. Le supprimer et recréer ?",
                    "Recréer",
                    "Annuler"))
                return;

            Object.DestroyImmediate(GameObject.Find(RootName));
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Raptor Attack Cinematic");

        Transform waypointsRoot = CreateChild(root.transform, "Waypoints");
        Transform camerasRoot = CreateChild(root.transform, "Cameras");
        Transform splinesRoot = CreateChild(root.transform, "Splines");
        Transform actorsRoot = CreateChild(root.transform, "Actors");
        Transform targetsRoot = CreateChild(root.transform, "CameraTargets");

        Transform pachyWalkStart = CreateWaypoint(waypointsRoot, "Pachy_WalkStart", origin + new Vector3(-12f, 0f, -8f));
        Transform pachyRiver = CreateWaypoint(waypointsRoot, "Pachy_RiverPoint", origin + new Vector3(0f, 0f, 6f));
        Transform raptorRunStart = CreateWaypoint(waypointsRoot, "Raptor_RunStart", origin + new Vector3(18f, 0f, -14f));
        Transform raptorRunMid = CreateWaypoint(waypointsRoot, "Raptor_RunMid", origin + new Vector3(8f, 0f, -4f));
        Transform raptorRunEnd = CreateWaypoint(waypointsRoot, "Raptor_RunEnd", origin + new Vector3(-2f, 0f, 2f));
        Transform leapPoint = CreateWaypoint(waypointsRoot, "Raptor_LeapPoint", origin + new Vector3(3f, 0f, 5f));

        GameObject pachy = SpawnOrFindActor(actorsRoot, "Pachy_Cinematic", PachyPrefabPath, pachyWalkStart);
        DinoCinematicPerformer pachyPerformer = pachy.GetComponent<DinoCinematicPerformer>() ??
                                                pachy.AddComponent<DinoCinematicPerformer>();

        var raptorPerformers = new List<DinoCinematicPerformer>();
        Vector3[] raptorOffsets =
        {
            new Vector3(2f, 0f, -2f),
            new Vector3(4f, 0f, -4f),
            new Vector3(6f, 0f, -3f)
        };

        for (int i = 0; i < raptorOffsets.Length; i++)
        {
            Transform spawn = CreateWaypoint(actorsRoot, $"Raptor_Spawn_{i + 1}", raptorRunStart.position + raptorOffsets[i]);
            GameObject raptor = SpawnOrFindActor(actorsRoot, $"Raptor_Cinematic_{i + 1}", RaptorPrefabPath, spawn);
            raptor.SetActive(false);
            raptorPerformers.Add(raptor.GetComponent<DinoCinematicPerformer>() ??
                                 raptor.AddComponent<DinoCinematicPerformer>());
        }

        Transform leapSpawn = CreateWaypoint(actorsRoot, "Raptor_Leap", leapPoint.position, leapPoint.rotation);
        GameObject leapRaptor = SpawnOrFindActor(actorsRoot, "Raptor_Leap", RaptorPrefabPath, leapSpawn);
        leapRaptor.SetActive(false);
        DinoCinematicPerformer leapPerformer = leapRaptor.GetComponent<DinoCinematicPerformer>() ??
                                               leapRaptor.AddComponent<DinoCinematicPerformer>();

        Transform pachyFeet = CreateTarget(targetsRoot, "Pachy_FeetTarget", pachy.transform, new Vector3(0f, 0.45f, 0.2f));
        Transform pachyHead = CreateTarget(targetsRoot, "Pachy_HeadTarget", pachy.transform, new Vector3(0f, 1.6f, 0.4f));
        Transform raptorFeet = CreateTarget(targetsRoot, "Raptor_FeetTarget", raptorPerformers[0].transform, new Vector3(0f, 0.35f, 0f));

        SplineContainer openingSpline = CreateOpeningSpline(splinesRoot, origin);
        SplineContainer encircleSpline = CreateEncircleSpline(splinesRoot, pachyRiver.position);

        CinemachineBrain brain = FindOrCreateBrain();

        var shotCameras = new Dictionary<CinematicShotDefinition.ShotId, CinemachineCamera>();

        shotCameras[CinematicShotDefinition.ShotId.OpeningLandscape] =
            CreateOpeningCamera(camerasRoot, openingSpline, origin);

        shotCameras[CinematicShotDefinition.ShotId.PachyFeetWalk] =
            CreateFeetCamera(camerasRoot, "CM_02_PachyFeet", pachyFeet, pachyFeet, new Vector3(-1.4f, 0.25f, -2.2f), 0.35f);

        shotCameras[CinematicShotDefinition.ShotId.RaptorFeetRun] =
            CreateFeetCamera(camerasRoot, "CM_03_RaptorFeet", raptorFeet, raptorFeet, new Vector3(1.6f, 0.2f, -1.8f), 1f);

        shotCameras[CinematicShotDefinition.ShotId.PachyDrink] =
            CreateFeetCamera(camerasRoot, "CM_04_PachyDrink", pachyFeet, pachyFeet, new Vector3(-2f, 0.3f, -1.5f), 0.15f);

        shotCameras[CinematicShotDefinition.ShotId.PachyAlert] =
            CreateAlertCamera(camerasRoot, pachyFeet, pachyHead);

        var orbitals = new List<CinematicOrbitalDriver>();
        shotCameras[CinematicShotDefinition.ShotId.EncirclementA] =
            CreateOrbitCamera(camerasRoot, "CM_06_Encircle_A", pachyRiver, 0f, orbitals);
        shotCameras[CinematicShotDefinition.ShotId.EncirclementB] =
            CreateOrbitCamera(camerasRoot, "CM_06_Encircle_B", pachyRiver, 120f, orbitals);
        shotCameras[CinematicShotDefinition.ShotId.EncirclementC] =
            CreateOrbitCamera(camerasRoot, "CM_06_Encircle_C", pachyRiver, 240f, orbitals);

        shotCameras[CinematicShotDefinition.ShotId.FinalAttack] =
            CreateFeetCamera(camerasRoot, "CM_07_FinalAttack", leapSpawn, pachyHead, new Vector3(-3f, 1.2f, -2f), 0.45f);

        CinematicCutController cutController = CreateFadeCanvas(root.transform);

        RaptorAttackCinematicDirector director = root.AddComponent<RaptorAttackCinematicDirector>();
        List<CinematicShotDefinition> shots = BuildDefaultShots(shotCameras);

        AssignDirector(
            director,
            brain,
            cutController,
            pachyPerformer,
            raptorPerformers,
            leapPerformer,
            pachyWalkStart,
            pachyRiver,
            new[] { raptorRunStart, raptorRunMid, raptorRunEnd },
            pachyFeet,
            pachyHead,
            raptorFeet,
            leapPoint,
            shotCameras,
            orbitals);

        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log(
            "Cinématique Raptor Attack créée. Ajustez les waypoints/splines dans la scène, puis lancez Play ou clic droit → Play Cinematic sur CinematicDirector.",
            root);
    }

    private static List<CinematicShotDefinition> BuildDefaultShots(Dictionary<CinematicShotDefinition.ShotId, CinemachineCamera> cameras)
    {
        return new List<CinematicShotDefinition>
        {
            CreateShot(cameras, CinematicShotDefinition.ShotId.OpeningLandscape, "01 - Ouverture", 8f, 2f),
            CreateShot(cameras, CinematicShotDefinition.ShotId.PachyFeetWalk, "02 - Pieds Pachy", 6f, 1.8f),
            CreateShot(cameras, CinematicShotDefinition.ShotId.RaptorFeetRun, "03 - Pieds Raptors", 4f, 1.2f),
            CreateShot(cameras, CinematicShotDefinition.ShotId.PachyDrink, "04 - Boisson", 5f, 1.5f),
            CreateShot(cameras, CinematicShotDefinition.ShotId.PachyAlert, "05 - Alerte", 2.5f, 0.8f),
            CreateShot(cameras, CinematicShotDefinition.ShotId.EncirclementA, "06A - Encerclement", 2f, 0.6f),
            CreateShot(cameras, CinematicShotDefinition.ShotId.EncirclementB, "06B - Encerclement", 2f, 0.5f),
            CreateShot(cameras, CinematicShotDefinition.ShotId.EncirclementC, "06C - Encerclement", 2f, 0.5f),
            CreateShot(cameras, CinematicShotDefinition.ShotId.FinalAttack, "07 - Attaque", 2.2f, 0.4f,
                CinemachineBlendDefinition.Styles.Cut)
        };
    }

    private static CinematicShotDefinition CreateShot(
        Dictionary<CinematicShotDefinition.ShotId, CinemachineCamera> cameras,
        CinematicShotDefinition.ShotId id,
        string name,
        float duration,
        float blend,
        CinemachineBlendDefinition.Styles style = CinemachineBlendDefinition.Styles.EaseInOut)
    {
        return new CinematicShotDefinition
        {
            shotId = id,
            displayName = name,
            virtualCamera = cameras[id],
            duration = duration,
            blendInDuration = blend,
            blendStyle = style
        };
    }

    private static void AssignDirector(
        RaptorAttackCinematicDirector director,
        CinemachineBrain brain,
        CinematicCutController cutController,
        DinoCinematicPerformer pachyPerformer,
        List<DinoCinematicPerformer> raptorPerformers,
        DinoCinematicPerformer leapPerformer,
        Transform pachyWalkStart,
        Transform pachyRiver,
        Transform[] raptorRunWaypoints,
        Transform pachyFeet,
        Transform pachyHead,
        Transform raptorFeet,
        Transform leapPoint,
        Dictionary<CinematicShotDefinition.ShotId, CinemachineCamera> cameras,
        List<CinematicOrbitalDriver> orbitals)
    {
        SerializedObject so = new SerializedObject(director);

        so.FindProperty("cinemachineBrain").objectReferenceValue = brain;
        so.FindProperty("cutController").objectReferenceValue = cutController;
        so.FindProperty("pachyPerformer").objectReferenceValue = pachyPerformer;
        so.FindProperty("leapRaptor").objectReferenceValue = leapPerformer;
        so.FindProperty("pachyWalkStart").objectReferenceValue = pachyWalkStart;
        so.FindProperty("pachyRiverPoint").objectReferenceValue = pachyRiver;
        so.FindProperty("pachyFeetTarget").objectReferenceValue = pachyFeet;
        so.FindProperty("pachyHeadTarget").objectReferenceValue = pachyHead;
        so.FindProperty("raptorFeetTarget").objectReferenceValue = raptorFeet;
        so.FindProperty("leapTargetPoint").objectReferenceValue = leapPoint;

        SetAnimationClips(so);
        SetArray(so, "raptorPerformers", raptorPerformers.ToArray());
        SetArray(so, "raptorRunWaypoints", raptorRunWaypoints);
        SetArray(so, "encircleOrbitals", orbitals.ToArray());
        SetShotList(so, BuildDefaultShots(cameras));

        CinemachineCamera openingCam = cameras[CinematicShotDefinition.ShotId.OpeningLandscape];
        so.FindProperty("openingDolly").objectReferenceValue = openingCam.GetComponent<CinematicDollyDriver>();
        so.FindProperty("openingShake").objectReferenceValue = openingCam.GetComponentInChildren<CinematicCameraShake>();

        so.FindProperty("pachyFeetShake").objectReferenceValue =
            cameras[CinematicShotDefinition.ShotId.PachyFeetWalk].GetComponentInChildren<CinematicCameraShake>();
        so.FindProperty("raptorFeetShake").objectReferenceValue =
            cameras[CinematicShotDefinition.ShotId.RaptorFeetRun].GetComponentInChildren<CinematicCameraShake>();
        so.FindProperty("alertHeightDriver").objectReferenceValue =
            cameras[CinematicShotDefinition.ShotId.PachyAlert].GetComponentInChildren<CinematicCameraHeightDriver>();

        so.FindProperty("playOnStart").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetAnimationClips(SerializedObject so)
    {
        so.FindProperty("pachyWalkClip").objectReferenceValue =
            LoadClip("Assets/Animation/Dinosaure/Pachycephalasaurus/Armature_Pachycephalasaurus_Walk.anim");
        so.FindProperty("pachyDrinkClip").objectReferenceValue =
            LoadClip("Assets/Animation/Dinosaure/Pachycephalasaurus/Armature_Pachycephalasaurus_Drink.anim");
        so.FindProperty("pachyAlertClip").objectReferenceValue =
            LoadClip("Assets/Animation/Dinosaure/Pachycephalasaurus/Armature_Pachycephalasaurus_Ready.anim");
        so.FindProperty("raptorRunClip").objectReferenceValue =
            LoadClip("Assets/Animation/Dinosaure/Velociraptor/RaptorArmature_Raptor_Run1_Anim.anim");
        so.FindProperty("raptorLeapClip").objectReferenceValue =
            LoadClip("Assets/Animation/Dinosaure/Velociraptor/RaptorArmature_Raptor_Leap1_Anim_RM.anim");
    }

    private static AnimationClip LoadClip(string path) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

    private static CinemachineCamera CreateOpeningCamera(Transform parent, SplineContainer spline, Vector3 origin)
    {
        GameObject rig = new GameObject("CM_01_Opening");
        rig.transform.SetParent(parent, false);
        rig.transform.position = origin + new Vector3(-20f, 1.2f, -18f);

        GameObject shakePivot = CreateChild(rig.transform, "ShakePivot").gameObject;
        CinematicCameraShake shake = shakePivot.AddComponent<CinematicCameraShake>();

        CinemachineCamera vcam = shakePivot.AddComponent<CinemachineCamera>();
        ConfigureLens(vcam, 55f);

        CinemachineSplineDolly dolly = shakePivot.AddComponent<CinemachineSplineDolly>();
        SetSplineDolly(dolly, spline, new Vector3(0f, -0.8f, 0f), 0.06f);

        CinematicDollyDriver driver = shakePivot.AddComponent<CinematicDollyDriver>();
        SetPrivateField(driver, "travelSpeed", 0.06f);

        CinemachineRotationComposer composer = shakePivot.AddComponent<CinemachineRotationComposer>();
        SetComposerOffset(composer, new Vector3(0f, 0.5f, 0f));

        vcam.Priority = new PrioritySettings { Enabled = false, Value = 0 };
        return vcam;
    }

    private static CinemachineCamera CreateFeetCamera(
        Transform parent,
        string name,
        Transform followTarget,
        Transform lookTarget,
        Vector3 offset,
        float shakeIntensity)
    {
        GameObject rig = new GameObject(name);
        rig.transform.SetParent(parent, false);

        CinematicFollowPivot pivot = rig.AddComponent<CinematicFollowPivot>();
        SetPrivateField(pivot, "target", followTarget);
        SetPrivateField(pivot, "lookAtTarget", lookTarget);
        SetPrivateField(pivot, "worldOffset", offset);

        GameObject shakePivot = CreateChild(rig.transform, "ShakePivot").gameObject;
        CinematicCameraShake shake = shakePivot.AddComponent<CinematicCameraShake>();
        SetPrivateField(shake, "positionAmplitude", 0.03f * (shakeIntensity + 0.5f));
        SetPrivateField(shake, "rotationAmplitude", 0.4f * (shakeIntensity + 0.5f));

        CinemachineCamera vcam = shakePivot.AddComponent<CinemachineCamera>();
        ConfigureLens(vcam, 45f);
        vcam.Priority = new PrioritySettings { Enabled = false, Value = 0 };
        return vcam;
    }

    private static CinemachineCamera CreateAlertCamera(Transform parent, Transform feet, Transform head)
    {
        GameObject rig = new GameObject("CM_05_PachyAlert");
        rig.transform.SetParent(parent, false);

        CinematicFollowPivot pivot = rig.AddComponent<CinematicFollowPivot>();
        SetPrivateField(pivot, "target", feet);
        SetPrivateField(pivot, "lookAtTarget", head);
        SetPrivateField(pivot, "worldOffset", new Vector3(-1.8f, 0.35f, -1.6f));

        GameObject heightPivot = CreateChild(rig.transform, "HeightPivot").gameObject;
        CinematicCameraHeightDriver heightDriver = heightPivot.AddComponent<CinematicCameraHeightDriver>();
        SetPrivateField(heightDriver, "pivot", heightPivot.transform);
        SetPrivateField(heightDriver, "startHeight", 0.35f);
        SetPrivateField(heightDriver, "endHeight", 1.5f);

        GameObject shakePivot = CreateChild(heightPivot.transform, "ShakePivot").gameObject;
        shakePivot.AddComponent<CinematicCameraShake>();

        CinemachineCamera vcam = shakePivot.AddComponent<CinemachineCamera>();
        ConfigureLens(vcam, 48f);

        CinemachineRotationComposer composer = shakePivot.AddComponent<CinemachineRotationComposer>();
        SetComposerTarget(composer, head);
        SetComposerOffset(composer, Vector3.zero);

        vcam.Priority = new PrioritySettings { Enabled = false, Value = 0 };
        return vcam;
    }

    private static CinemachineCamera CreateOrbitCamera(
        Transform parent,
        string name,
        Transform target,
        float startAngle,
        List<CinematicOrbitalDriver> orbitals)
    {
        GameObject rig = new GameObject(name);
        rig.transform.SetParent(parent, false);

        CinematicOrbitalDriver orbital = rig.AddComponent<CinematicOrbitalDriver>();
        SetPrivateField(orbital, "target", target);
        SetPrivateField(orbital, "radius", 7f);
        SetPrivateField(orbital, "height", 0.55f);
        SetPrivateField(orbital, "startAngle", startAngle);
        orbitals.Add(orbital);

        GameObject shakePivot = CreateChild(rig.transform, "ShakePivot").gameObject;
        CinematicCameraShake shake = shakePivot.AddComponent<CinematicCameraShake>();
        SetPrivateField(shake, "positionAmplitude", 0.05f);
        SetPrivateField(shake, "rotationAmplitude", 0.8f);

        CinemachineCamera vcam = shakePivot.AddComponent<CinemachineCamera>();
        ConfigureLens(vcam, 50f);
        vcam.Priority = new PrioritySettings { Enabled = false, Value = 0 };
        return vcam;
    }

    private static SplineContainer CreateOpeningSpline(Transform parent, Vector3 origin)
    {
        GameObject go = new GameObject("Spline_Opening");
        go.transform.SetParent(parent, false);

        SplineContainer container = go.AddComponent<SplineContainer>();
        Spline spline = new Spline();
        spline.Add(new BezierKnot(origin + new Vector3(-22f, 1.2f, -20f)));
        spline.Add(new BezierKnot(origin + new Vector3(-10f, 1f, -8f)));
        spline.Add(new BezierKnot(origin + new Vector3(2f, 0.9f, 2f)));
        spline.Add(new BezierKnot(origin + new Vector3(8f, 0.8f, 8f)));
        container.AddSpline(spline);
        return container;
    }

    private static SplineContainer CreateEncircleSpline(Transform parent, Vector3 center)
    {
        GameObject go = new GameObject("Spline_Encircle");
        go.transform.SetParent(parent, false);

        SplineContainer container = go.AddComponent<SplineContainer>();
        var knots = new List<BezierKnot>();
        for (int i = 0; i <= 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Sin(angle) * 7f, 0.6f, Mathf.Cos(angle) * 7f);
            knots.Add(new BezierKnot(pos));
        }

        Spline spline = new Spline();
        foreach (BezierKnot knot in knots)
            spline.Add(knot);
        container.AddSpline(spline);
        return container;
    }

    private static CinematicCutController CreateFadeCanvas(Transform parent)
    {
        GameObject canvasGo = new GameObject("CinematicFade");
        canvasGo.transform.SetParent(parent, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject imageGo = new GameObject("Black");
        imageGo.transform.SetParent(canvasGo.transform, false);
        Image image = imageGo.AddComponent<Image>();
        image.color = Color.black;

        RectTransform rt = image.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        CanvasGroup group = imageGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        CinematicCutController cut = canvasGo.AddComponent<CinematicCutController>();
        SetPrivateField(cut, "fadeGroup", group);
        return cut;
    }

    private static CinemachineBrain FindOrCreateBrain()
    {
        CinemachineBrain brain = Object.FindFirstObjectByType<CinemachineBrain>();
        if (brain != null)
        {
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.5f);
            return brain;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }

        brain = cam.GetComponent<CinemachineBrain>();
        if (brain == null)
            brain = cam.gameObject.AddComponent<CinemachineBrain>();

        brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 1.5f);
        return brain;
    }

    private static GameObject SpawnOrFindActor(Transform parent, string name, string prefabPath, Transform spawn)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance;

        if (prefab != null)
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
        }
        else
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            instance.name = name + "_Placeholder";
            instance.transform.SetParent(parent, false);
            Debug.LogWarning($"Prefab introuvable : {prefabPath}. Placeholder utilisé.");
        }

        instance.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        DisableGameplayComponents(instance);
        return instance;
    }

    private static void DisableGameplayComponents(GameObject instance)
    {
        if (instance.TryGetComponent(out UnityEngine.AI.NavMeshAgent agent))
            agent.enabled = false;

        foreach (MonoBehaviour behaviour in instance.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null)
                continue;

            string typeName = behaviour.GetType().Name;
            if (typeName.Contains("AI") || typeName.Contains("Health"))
                behaviour.enabled = false;
        }
    }

    private static Transform CreateWaypoint(Transform parent, string name, Vector3 position, Quaternion? rotation = null)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        if (rotation.HasValue)
            go.transform.rotation = rotation.Value;

        return go.transform;
    }

    private static Transform CreateTarget(Transform parent, string name, Transform follow, Vector3 localOffset)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(follow, false);
        go.transform.localPosition = localOffset;
        go.transform.localRotation = Quaternion.identity;
        return go.transform;
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Vector3 FindSceneCenter()
    {
        if (Selection.activeTransform != null)
            return Selection.activeTransform.position;

        var pachy = GameObject.Find("Pachycephalasaurus_24K");
        if (pachy != null)
            return pachy.transform.position;

        return Vector3.zero;
    }

    private static void ConfigureLens(CinemachineCamera vcam, float fov)
    {
        LensSettings lens = vcam.Lens;
        lens.FieldOfView = fov;
        vcam.Lens = lens;
    }

    private static void SetSplineDolly(CinemachineSplineDolly dolly, SplineContainer spline, Vector3 offset, float speed)
    {
        var settings = dolly.SplineSettings;
        settings.Spline = spline;
        settings.Position = 0f;
        dolly.SplineSettings = settings;
        dolly.SplineOffset = offset;
        dolly.AutomaticDolly.Enabled = true;

        if (dolly.AutomaticDolly.Method is SplineAutoDolly.FixedSpeed fixedSpeed)
            fixedSpeed.Speed = speed;
    }

    private static void SetComposerOffset(CinemachineRotationComposer composer, Vector3 offset)
    {
        composer.TargetOffset = offset;
    }

    private static void SetComposerTarget(CinemachineRotationComposer composer, Transform target)
    {
        var cam = composer.GetComponent<CinemachineCamera>();
        if (cam != null)
        {
            var targetSettings = cam.Target;
            targetSettings.LookAtTarget = target;
            targetSettings.CustomLookAtTarget = true;
            cam.Target = targetSettings;
        }
    }

    private static void SetPrivateField(Object obj, string fieldName, object value)
    {
        FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        field?.SetValue(obj, value);
    }

    private static void SetArray(SerializedObject so, string propertyName, Object[] values)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void SetShotList(SerializedObject so, List<CinematicShotDefinition> shots)
    {
        SerializedProperty prop = so.FindProperty("shots");
        prop.arraySize = shots.Count;

        for (int i = 0; i < shots.Count; i++)
        {
            SerializedProperty element = prop.GetArrayElementAtIndex(i);
            CinematicShotDefinition shot = shots[i];
            element.FindPropertyRelative("shotId").enumValueIndex = (int)shot.shotId;
            element.FindPropertyRelative("displayName").stringValue = shot.displayName;
            element.FindPropertyRelative("virtualCamera").objectReferenceValue = shot.virtualCamera;
            element.FindPropertyRelative("duration").floatValue = shot.duration;
            element.FindPropertyRelative("blendInDuration").floatValue = shot.blendInDuration;
            element.FindPropertyRelative("blendStyle").enumValueIndex = (int)shot.blendStyle;
        }
    }
}
#endif
