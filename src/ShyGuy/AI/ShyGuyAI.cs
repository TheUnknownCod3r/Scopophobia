using GameNetcodeStuff;
using LethalLib.Modules;
using Scopophobia;
using Scopophobia.Dependencies;
using Scopophobia.ShyGuy.AI;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

namespace ShyGuy.AI
{
    public class ShyGuyAI : EnemyAI
    {
        private Transform localPlayerCamera;

        public Vector3 mainEntrancePosition;

        public Collider mainCollider;
        public static bool canSeeFace;
        public VehicleController CompanyCruiser;
        public bool pryingOpenDoor;
        public HangarShipDoor shipDoor;

        private float pryingDoorAnimTime;

        public float pryOpenDoorAnimLength;

        public AudioClip breakAndEnter;

        public AudioClip shipAlarm;

        public AudioSource breakDownDoorAudio;

        public AudioSource farAudio;
        public AudioSource footstepSource;

        public AudioClip screamSFX;

        public AudioClip panicSFX;

        public AudioClip crySFX;

        public AudioClip crySittingSFX;

        public AudioClip killPlayerSFX;

        [Header("Containment Breach Sounds")]
        public AudioClip screamSFX_CB;

        public AudioClip panicSFX_CB;

        public AudioClip crySFX_CB;

        public AudioClip killPlayerSFX_CB;

        [Header("Alpha Containment Breach Sounds")]
        public AudioClip screamSFX_ACB;

        public AudioClip panicSFX_ACB;

        public AudioClip crySFX_ACB;

        public AudioClip killPlayerSFX_ACB;

        [Header("Secret Laboratory Sounds")]
        public AudioClip screamSFX_SL;

        public AudioClip panicSFX_SL;

        public AudioClip crySFX_SL;

        public AudioClip killPlayerSFX_SL;
        private int currentClipID = -1;
        private NetworkVariable<int> syncedAudioClipID = new NetworkVariable<int>(-1,NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public Material bloodyMaterial;
        public AISearchRoutine roamMap;

        public Transform shyGuyFace;

        private Vector3 spawnPosition;

        private Vector3 previousPosition;

        private int previousState = -1;
        private bool canBreakIntoShip;
        private float roamWaitTime = 40f;

        [Header("Teleports")]
        public static EntranceTeleport[] entranceTeleports;
        public static List<EntranceTeleport> outsideTeleports = [];
        public static List<EntranceTeleport> insideTeleports = [];
        public bool pathingToTeleport;
        public Vector3 closestTeleportPosition;
        public Vector3 ClosestTeleportExitPos;
        public static EntranceTeleport mainEntrance;
        public EntranceTeleport? closestTeleport;

        private bool roamShouldSit;

        private bool sitting;

        private float lastRunSpeed;

        private float seeFaceTime;

        private float triggerTime;

        public float triggerDuration = 66.4f;

        private float timeToTrigger = 0.5f;

        private NavMeshPath? pathToTeleport;
        private float lastInterval;

        private bool inKillAnimation;
        public DoorCollisionDetectHook? doorCollisionDetectScriptHook;
        private bool isInElevatorStartRoom;
        public bool hasBeenSpawned;
        private float timeAtLastUsingEntrance;
        public static MineshaftElevatorController elevatorScript;
        public List<PlayerControllerB> SCP096Targets = new List<PlayerControllerB>();
        public override void Start()
        {
            base.Start();
            if (CompanyCruiser == null) CompanyCruiser = FindObjectOfType<VehicleController>();
            if (shipDoor == null) shipDoor = FindObjectOfType<HangarShipDoor>();
            if (agent == null) agent = GetComponentInChildren<NavMeshAgent>();
            if(!hasBeenSpawned) hasBeenSpawned = true;
            if (elevatorScript == null) elevatorScript = FindObjectOfType<MineshaftElevatorController>();//Lets see if the Elevator Controls exist, I hope they do, we're on the mineshaft
            triggerDuration = Config.triggerTime;
            canBreakIntoShip = Config.canBreakIntoShip;
            pathToTeleport = new NavMeshPath();
            //if(Config.RandomSpawnSizes){ this.transform.localScale = new Vector3(Random.Range(0, 1), Random.Range(0, 1), Random.Range(0, 1)); }
            Transform leftEye = null;
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(transform);
            while (queue.Count > 0)
            {
                Transform c = queue.Dequeue();
                if (c.name == "lefteye")
                {
                    leftEye = c;
                    break;
                }
                foreach (Transform t in c)
                {
                    queue.Enqueue(t);
                }
            }
            Transform rightEye = null;
            queue = new Queue<Transform>();
            queue.Enqueue(transform);
            while (queue.Count > 0)
            {
                Transform c = queue.Dequeue();
                if (c.name == "righteye")
                {
                    rightEye = c;
                    break;
                }
                foreach (Transform t in c)
                {
                    queue.Enqueue(t);
                }
            }
            if (!Config.hasGlowingEyes && leftEye != null && rightEye != null)
            {
                leftEye.gameObject.SetActive(value: false);
                rightEye.gameObject.SetActive(value: false);
            }
            
            if (Config.bloodyTexture && bloodyMaterial != null)
            {
                ApplyBloodyMaterial();
            }
            switch (Config.soundPack)
            {
                case "SCPCB":
                    screamSFX = screamSFX_CB;
                    crySFX = crySFX_CB;
                    crySittingSFX = crySFX_CB;
                    panicSFX = panicSFX_CB;
                    killPlayerSFX = killPlayerSFX_CB;
                    break;
                case "SCPCBOld":
                    screamSFX = screamSFX_ACB;
                    crySFX = crySFX_ACB;
                    crySittingSFX = crySFX_ACB;
                    panicSFX = panicSFX_ACB;
                    killPlayerSFX = killPlayerSFX_ACB;
                    break;
                case "SecretLab":
                    screamSFX = screamSFX_SL;
                    crySFX = crySFX_SL;
                    crySittingSFX = crySFX_SL;
                    panicSFX = panicSFX_SL;
                    killPlayerSFX = killPlayerSFX_SL;
                    break;
            }
            localPlayerCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform;
            spawnPosition = transform.position;
            isOutside = transform.position.y > -80f;
            mainEntrance = RoundManager.FindMainEntranceScript(isOutside);
            mainEntrancePosition = RoundManager.Instance.GetNavMeshPosition(RoundManager.FindMainEntrancePosition(getTeleportPosition: true, isOutside));
            path1 = new NavMeshPath();
            //the Following Code is adapted on code from StarlancerAIFix. 
            outsideTeleports.Clear();
            insideTeleports.Clear();
            entranceTeleports = UnityEngine.Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None); //Load all Teleports
            for (int i = 0; i < entranceTeleports.Length; i++)
            {
                int entranceID = entranceTeleports[i].entranceId;

                if (entranceTeleports[i].isEntranceToBuilding)
                {
                    if (!entranceTeleports[i].FindExitPoint())
                    {
                        continue;
                    }
                    else if (entranceTeleports[i].entrancePoint == null)
                    {
                        continue;
                    }

                    outsideTeleports.Add(entranceTeleports[i]);
                    outsideTeleports.Sort((entranceA, entranceB) => entranceA.entranceId.CompareTo(entranceB.entranceId));
                }
                else
                {
                    if (!entranceTeleports[i].FindExitPoint())
                    {
                        continue;
                    }
                    else if (entranceTeleports[i].entrancePoint == null)
                    {
                        continue;
                    }

                    insideTeleports.Add(entranceTeleports[i]);
                    insideTeleports.Sort((entranceA, entranceB) => entranceA.entranceId.CompareTo(entranceB.entranceId));
                }
            }
            //End Code from StarlancerAIFix
            GetAINodes();
            if (isOutside)
            {
                if (allAINodes == null || allAINodes.Length == 0)
                {
                    allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
                }
            }
            else if (allAINodes == null || allAINodes.Length == 0)
            {
                allAINodes = GameObject.FindGameObjectsWithTag("AINode");
            }
            openDoorSpeedMultiplier = 450f;
            SetShyGuyInitialValues();
        }

        private void CalculateAnimationSpeed()
        {
            float num = (transform.position - previousPosition).magnitude;
            if (num > 0f)
            {
                num = 1f;
            }
            lastRunSpeed = Mathf.Lerp(lastRunSpeed, num, 5f * Time.deltaTime);
            creatureAnimator.SetFloat("VelocityZ", lastRunSpeed);
            previousPosition = transform.position;
        }

        public override void DoAIInterval()
        {
            base.DoAIInterval();
            if (StartOfRound.Instance.livingPlayers == 0)
            {
                lastInterval = Time.realtimeSinceStartup;
                return;
            }
            if (isEnemyDead)
            {
                lastInterval = Time.realtimeSinceStartup;
                return;
            }
            if (!IsServer && IsOwner && currentBehaviourStateIndex != 2)
            {
                ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
            }
            switch (currentBehaviourStateIndex)
            {
                case 0:
                    {
                        if (stunNormalizedTimer > 0f || sitting)
                        {
                            agent.speed = 0f;
                        }
                        else
                        {
                            roamWaitTime -= Time.realtimeSinceStartup - lastInterval;
                            openDoorSpeedMultiplier = 1f;
                            agent.speed = 2.75f * Config.speedDocileMultiplier;
                        }
                        movingTowardsTargetPlayer = false;
                        agent.stoppingDistance = 4f;
                        addPlayerVelocityToDestination = 0f;
                        PlayerControllerB targetPlayer = base.targetPlayer;
                        if (roamWaitTime <= 20f && roamMap.inProgress && targetPlayer == null)
                        {
                            StopSearch(roamMap);
                            lastInterval = Time.realtimeSinceStartup;
                        }
                        else if (roamWaitTime > 2.5f && roamWaitTime <= 15f && !roamMap.inProgress && targetPlayer == null && roamShouldSit)
                        {
                            sitting = true;
                            creatureAnimator.SetBool("Sitting", value: true);
                            float preTime = creatureVoice.time;
                            creatureVoice.volume = 0.3f;
                            creatureVoice.clip = crySittingSFX;
                            creatureVoice.Play();
                            creatureVoice.time = preTime;
                            lastInterval = Time.realtimeSinceStartup;
                        }
                        else if (!(base.targetPlayer != null) && targetPlayer == null && !roamMap.inProgress && roamWaitTime <= 0f)
                        {
                            if (!sitting)
                            {
                                roamShouldSit = Random.Range(1, 5) == 1;
                                roamWaitTime = Random.Range(25f, 32.5f);
                                StartSearch(spawnPosition, roamMap);
                                lastInterval = Time.realtimeSinceStartup;
                                break;
                            }
                            sitting = false;
                            creatureVoice.Stop();
                            roamShouldSit = false;
                            roamWaitTime = Random.Range(21f, 25f);
                            creatureAnimator.SetBool("Sitting", value: false);
                            float preTime = creatureVoice.time;
                            creatureVoice.volume = 0.3f;
                            creatureVoice.clip = crySFX;
                            creatureVoice.Play();
                            creatureVoice.time = preTime;
                            lastInterval = Time.realtimeSinceStartup;
                        }
                        break;
                    }
                case 1:
                    agent.speed = 0f;
                    lastInterval = Time.realtimeSinceStartup;
                    movingTowardsTargetPlayer = false;
                    break;
                case 2:
                    {
                        agent.stoppingDistance = 0f;
                        agent.avoidancePriority = 60;
                        openDoorSpeedMultiplier = 450f;
                        mainCollider.isTrigger = true;
                        addPlayerVelocityToDestination = 1f;
                        if (inKillAnimation)
                        {
                            agent.speed = 0f;
                        }
                        else
                        {
                            agent.speed = Mathf.Clamp(agent.speed + (Time.realtimeSinceStartup - lastInterval) * Config.speedRageMultiplier * 1.1f, 5f * Config.speedRageMultiplier, 14.75f * Config.speedRageMultiplier);
                        }
                        if (SCP096Targets.Count <= 0)
                        {
                            SitDown();
                            break;
                        }
                        PlayerControllerB oldTargetPlayer = base.targetPlayer;
                        float closestDist = float.PositiveInfinity;

                        foreach (PlayerControllerB hunted in SCP096Targets)
                        {
                            bool sameArea = hunted.isInsideFactory == !isOutside;
                            bool allowedToLeave = true;
                            if (!Config.canExitFacility && !sameArea)
                            {
                                allowedToLeave = false;
                            }
                            if (!hunted.isPlayerDead && allowedToLeave)
                            {
                                float distance = Vector3.Distance(hunted.transform.position, transform.position);
                                if (PlayerIsTargetable2(hunted, true, false) && Vector3.Distance(hunted.transform.position, transform.position) < closestDist)
                                //if (!hunted.isPlayerDead && hunted.isPlayerControlled && hunted.inAnimationWithEnemy == null && hunted.sinkingValue < 0.73f && distance < float.PositiveInfinity)
                                {
                                    closestDist = Vector3.Magnitude(hunted.transform.position - transform.position);
                                    targetPlayer = hunted;
                                }
                            }
                            else
                            {
                                AddTargetToList((int)hunted.actualClientId, remove: true, "Removal");
                            }
                        }
                        if (targetPlayer != null)
                        {
                            creatureAnimator.SetFloat("DistanceToTarget", Vector3.Distance(transform.position, targetPlayer.transform.position));
                            if (roamMap.inProgress)
                            {
                                StopSearch(roamMap);
                            }
                            if (targetPlayer != oldTargetPlayer)
                            {
                                ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
                            }
                            if (isOutside && targetPlayer.isInHangarShipRoom && !pryingOpenDoor)
                            {//try breaking into ship
                                if (BreakIntoShip()) break;
                            }
                            if (!isOutside && elevatorScript != null && agent.CalculatePath(targetPlayer.transform.position, path1) && path1.status != NavMeshPathStatus.PathComplete)//Checks if Enemy is inside, if true, checks a NavMeshPath against the player position, if cannot reach player, heads to Elevator
                            {
                                //ScopophobiaPlugin.logger.LogInfo("Starting Elevator Checks");
                                if (Vector3.Distance(transform.position, elevatorScript.elevatorBottomPoint.position) < 7f)//Check distance from Bottom Button
                                {
                                    isInElevatorStartRoom = false;
                                    //ScopophobiaPlugin.logger.LogInfo("Shy guy is at Lower Elevator");
                                }
                                else if (Vector3.Distance(transform.position, elevatorScript.elevatorTopPoint.position) < 7f)//Another Distance check, this time for Top Button.
                                {
                                    isInElevatorStartRoom = true;
                                    //ScopophobiaPlugin.logger.LogInfo("Shy guy is at Upper Elevator");
                                }
                                bool goUp = !isInElevatorStartRoom;
                                bool shouldUseElevator = goUp ? Vector3.Distance(targetPlayer.transform.position, mainEntrancePosition) < 14f || !targetPlayer.isInsideFactory : !targetPlayer.isPlayerDead && targetPlayer.isPlayerControlled && targetPlayer.isInsideFactory && Vector3.Distance(targetPlayer.transform.position, mainEntrancePosition) > 12f;
                                if (shouldUseElevator){
                                    UseElevator(goUp); break;
                                }
                            }
                            if (targetPlayer.isInsideFactory != !isOutside)
                            {
                                if (!pathingToTeleport)
                                {
                                    ScopophobiaPlugin.Instance.LogInfoExtended($"{targetPlayer.name} Is Outside: {!targetPlayer.isInsideFactory}, Looking for Teleport");

                                    GetClosestTeleportAndMove();
                                    return;
                                }
                                if (Vector3.Distance(transform.position, closestTeleport.entrancePoint.position) < 3f)
                                {
                                    TeleAndRefreshEnemy(closestTeleport.exitScript.entrancePoint.position,!isOutside);
                                    agent.speed = 0f;
                                    return;
                                }

                                movingTowardsTargetPlayer = false;
                                SetDestinationToPosition(closestTeleport.entrancePoint.position);

                                ScopophobiaPlugin.Instance.LogInfoExtended($"{targetPlayer.name} is not in area, heading to closest Tele: {closestTeleport.entranceId}");

                                return;
                            }
                            else//Player in sights, continuing attack strategy
                            {
                                if (PathIsIntersectedByLineOfSight(targetPlayer.transform.position, false, false, true) && agent.CalculatePath(targetPlayer.transform.position, path1) && path1.status == NavMeshPathStatus.PathComplete)
                                {
                                        agent.SetDestination(targetPlayer.transform.position);//doubt this is right, but we can try it
                                        //SetMovingTowardsTargetPlayer(targetPlayer);
                                }
                                else SetMovingTowardsTargetPlayer(targetPlayer);
                            }
                        }
                        else if (SCP096Targets.Count <= 0)
                        {
                            SitDown();
                        }
                        break;
                    }
                default:
                    lastInterval = Time.realtimeSinceStartup;
                    break;
            }
        }
        public void GetClosestTeleportAndMove()
        {
            if (pathingToTeleport)
                return;

            List<EntranceTeleport> teleports = isOutside ? outsideTeleports : insideTeleports;

            EntranceTeleport? bestTeleport = null;
            float shortestPathLength = float.MaxValue;

            foreach (EntranceTeleport tele in teleports)
            {
                if (!tele.FindExitPoint() || tele.entrancePoint == null)
                    continue;

                if (!agent.CalculatePath(tele.entrancePoint.position, pathToTeleport))
                    continue;

                if (pathToTeleport.status != NavMeshPathStatus.PathComplete)
                    continue;

                float pathLength = GetPathLength(pathToTeleport);

                if (pathLength < shortestPathLength)
                {
                    shortestPathLength = pathLength;
                    bestTeleport = tele;
                }
            }
            if (bestTeleport == null)
            {
                bestTeleport = mainEntrance;

                ScopophobiaPlugin.Instance.LogInfoExtended($"No valid teleport found. Falling back to main entrance: {mainEntrance.entranceId}");
            }
            else
            {
                ScopophobiaPlugin.Instance.LogInfoExtended($"Closest teleport found: {bestTeleport.entranceId} (Path Length: {shortestPathLength:F1}m)");
            }

            closestTeleport = bestTeleport;
            closestTeleportPosition = bestTeleport.entrancePoint.position;
            pathingToTeleport = true;
        }

        public void TeleAndRefreshEnemy(Vector3 Pos, bool setOutside)
        {
            timeAtLastUsingEntrance = Time.realtimeSinceStartup;
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(Pos, default(NavMeshHit), 1f, -1);
            ScopophobiaPlugin.Instance.LogInfoExtended($"Teleport Requested: {Pos} | NavMesh Result: {navMeshPosition}, Teleporting Outside: {setOutside}");
            //Pos.y = Pos.y + 3f;//is this needed? some maps cause a tp under the map door?
            if (IsOwner)
            {
                agent?.enabled = false;
                transform.position = navMeshPosition;
                agent?.enabled = true;
            }
            else
            {
                transform.position = navMeshPosition;
            }
            serverPosition = navMeshPosition;
            SetEnemyOutside(setOutside);
            if (closestTeleport?.doorAudios != null && closestTeleport.doorAudios.Length != 0)
            {
                closestTeleport.entrancePointAudio.PlayOneShot(closestTeleport.doorAudios[0]);
                WalkieTalkie.TransmitOneShotAudio(closestTeleport.entrancePointAudio, closestTeleport.doorAudios[0], 1f);
            }
            pathingToTeleport = false;
            closestTeleportPosition = Vector3.negativeInfinity;
        }
        private float GetPathLength(NavMeshPath path)
        {
            float length = 0f;

            if (path.corners.Length < 2)
                return length;

            for (int i = 1; i < path.corners.Length; i++)
            {
                length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }

            return length;
        }
        private bool UseElevator(bool goUp)
        {
            Vector3 vector = ((!goUp) ? elevatorScript.elevatorTopPoint.position : elevatorScript.elevatorBottomPoint.position);
            ScopophobiaPlugin.Instance.LogInfoExtended($"goUp: {goUp}");
            ScopophobiaPlugin.Instance.LogInfoExtended($"{elevatorScript.elevatorFinishedMoving}, {!PathIsIntersectedByLineOfSight(elevatorScript.elevatorInsidePoint.position, calculatePathDistance: false, avoidLineOfSight: false)}");
            if (elevatorScript.elevatorFinishedMoving && !PathIsIntersectedByLineOfSight(elevatorScript.elevatorInsidePoint.position, calculatePathDistance: false, avoidLineOfSight: false))
            {
                ScopophobiaPlugin.Instance.LogInfoExtended($"goUp: {goUp}, elevatormovingdown: {elevatorScript.elevatorMovingDown}");
                ScopophobiaPlugin.Instance.LogInfoExtended($"{elevatorScript.elevatorDoorOpen}, {Vector3.Distance(transform.position, elevatorScript.elevatorInsidePoint.position) < 1f}, {elevatorScript.elevatorMovingDown == goUp}");
                if (elevatorScript.elevatorDoorOpen && Vector3.Distance(transform.position, elevatorScript.elevatorInsidePoint.position) < 1f && elevatorScript.elevatorMovingDown == goUp)
                {
                    elevatorScript.PressElevatorButtonOnServer(requireFinishedMoving: true);
                }
                SetDestinationToPosition(elevatorScript.elevatorInsidePoint.position);
                return true;
            }
            else if (Vector3.Distance(transform.position, elevatorScript.elevatorInsidePoint.position) > 1f && !PathIsIntersectedByLineOfSight(vector, calculatePathDistance: false, avoidLineOfSight: false))
            {
                if (elevatorScript.elevatorDoorOpen && Vector3.Distance(transform.position, vector) < 1f && elevatorScript.elevatorMovingDown != goUp && !elevatorScript.elevatorCalled)
                {
                    elevatorScript.CallElevatorOnServer(goUp);
                }
                SetDestinationToPosition(vector);
                return true;
            }
            if (elevatorScript.elevatorFinishedMoving) { SetMovingTowardsTargetPlayer(targetPlayer); return false; }
            return false;
        }
        public override void Update()
        {
            if (isEnemyDead || GameNetworkManager.Instance == null)
                return;
            CalculateAnimationSpeed();
            if (pryingOpenDoor && inSpecialAnimation)
            {
                transform.position = Vector3.Lerp(transform.position, shipDoor.outsideDoorPoint.position, 7f * Time.deltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, shipDoor.outsideDoorPoint.rotation, 7f * Time.deltaTime);
                pryingDoorAnimTime = Mathf.Min(pryingDoorAnimTime + Time.deltaTime / pryOpenDoorAnimLength, 1f);
                creatureAnimator.SetFloat("pryOpenDoor", pryingDoorAnimTime);
                shipDoor.shipDoorsAnimator.SetFloat("pryOpenDoor", pryingDoorAnimTime);
                creatureAnimator.SetLayerWeight(0, Mathf.Max(0f, creatureAnimator.GetLayerWeight(0) - Time.deltaTime * 5f));
                if (pryingDoorAnimTime > 0.12f)
                {
                    EnableEnemyMesh(enable: true);
                }
                BreakIntoShip();
                return;
            }
            canSeeFace = GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(shyGuyFace.position, Config.faceTriggerRange, 45);
            if (canSeeFace)
            {
                float shyGuyFaceDifference = Quaternion.Angle(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.rotation, shyGuyFace.rotation);
                if (!(shyGuyFaceDifference <= 145f))
                {
                    canSeeFace = false;
                }
            }
            if (canSeeFace)
            {
                seeFaceTime += Time.deltaTime;
                if (seeFaceTime >= Config.faceTriggerGracePeriod)
                {
                    GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1.25f);
                    if (!Config.hasMaxTargets || SCP096Targets.Count < Config.maxTargets)
                    {
                        ScopophobiaPlugin.Instance.LogInfoExtended($"Adding {GameNetworkManager.Instance.localPlayerController.actualClientId} To Targets. Has Seen Face: {canSeeFace}");
                        AddTargetToList((int)GameNetworkManager.Instance.localPlayerController.playerClientId,remove:false,"Seen Face");
                    }
                    if (currentBehaviourStateIndex == 0)
                    {
                        ScopophobiaPlugin.Instance.LogInfoExtended($"Switching to triggered State");
                        SwitchToBehaviourState(1);
                    }
                }
            }
            else
            {
                seeFaceTime = Mathf.Clamp(seeFaceTime - Time.deltaTime, 0f, timeToTrigger);
                if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(transform.position + Vector3.up * 1f, 30f) && currentBehaviourStateIndex == 0)
                {
                    if (!thisNetworkObject.IsOwner)
                    {
                        ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
                    }
                    if (Vector3.Distance(transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 10f)
                    {
                        GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.65f);
                    }
                    else
                    {
                        GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.25f);
                    }
                }
            }
            switch (currentBehaviourStateIndex)
            {
                case 0:
                    if (previousState != 0)
                    {
                        SetShyGuyInitialValues();
                        previousState = 0;
                        mainCollider.isTrigger = true;
                        farAudio.volume = 0f;
                        if (sitting) PlayAudioFxOnLocalClient(0);
                        else PlayAudioFxOnLocalClient(1);
                    }
                    break;
                case 1:
                    if (previousState != 1)
                    {
                        previousState = 1;
                        sitting = false;
                        mainCollider.isTrigger = true;
                        creatureAnimator.SetBool("Rage", value: false);
                        creatureAnimator.SetBool("Sitting", value: false);
                        creatureAnimator.SetBool("triggered", value: true);
                        PlayAudioFxOnLocalClient(2);
                        agent.speed = 0f;
                        triggerTime = triggerDuration;
                    }
                    triggerTime -= Time.deltaTime;
                    if (triggerTime <= 0f)
                    {
                        SwitchToBehaviourState(2);
                    }
                    break;
                case 2:
                    if (previousState != 2)
                    {
                        mainCollider.isTrigger = true;
                        previousState = 2;
                        creatureAnimator.SetBool("Rage", value: true);
                        creatureAnimator.SetBool("triggered", value: false);
                        farAudio.Stop();
                        if (!creatureVoice.isPlaying || creatureVoice.clip != screamSFX)
                        {
                            PlayAudioFxOnLocalClient(3);
                        }
                    }
                    break;
            }
            base.Update();
        }


        public virtual bool PlayerIsTargetable2(PlayerControllerB playerScript,bool overrideInsideFactoryCheck = false,bool checkForMineshaftStartTile = true)
        {
            if (!isOutside && checkForMineshaftStartTile && RoundManager.Instance.currentDungeonType == 4 && !IsInMineshaftStartRoomWithPlayer(playerScript))
            {
                return false;
            }

            if (playerScript.isPlayerControlled && !playerScript.isPlayerDead && playerScript.inAnimationWithEnemy == null && (overrideInsideFactoryCheck || playerScript.isInsideFactory != isOutside) && playerScript.sinkingValue < 0.73f)
            {
                return true;
            }

            return false;
        }


        public void PlayAudioFxOnLocalClient(int audioClipID)
        {
            float volume = Mathf.Clamp01(Config.VolumeConfigs * 0.1f);

            switch (audioClipID)
            {
                case 0:if(farAudio.isPlaying)farAudio.Stop(); if (creatureVoice.isPlaying) creatureVoice.Stop(); creatureVoice.volume = Mathf.Clamp01(Config.VolumeConfigs); creatureVoice.clip = crySittingSFX;creatureVoice.time = 0f; creatureVoice.Play();break;
                case 1: creatureVoice.Stop(); creatureVoice.volume = Config.VolumeConfigs * 0.1f; creatureVoice.clip = crySFX;creatureVoice.time = 0f; creatureVoice.Play(); break;
                case 2: creatureVoice.Stop(); farAudio.volume = Config.VolumeConfigs * 0.1f; farAudio.clip = panicSFX; farAudio.time = 0f; farAudio.Play(); break;
                case 3: farAudio.Stop(); farAudio.volume = Config.VolumeConfigs * 0.1f - 0.1f; farAudio.clip = screamSFX; farAudio.time = 0f; farAudio.Play(); break;
            }
        }
        public new void SetEnemyOutside(bool outside = false)
        {
            isOutside = outside;
            mainEntrancePosition = RoundManager.Instance.GetNavMeshPosition(RoundManager.FindMainEntrancePosition(getTeleportPosition: true, outside));
            if (outside)
            {
                allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
            }
            else
            {
                allAINodes = GameObject.FindGameObjectsWithTag("AINode");
            }
        }

        public void BashDoor()
        {
            doorCollisionDetectScriptHook?.BashDoor();
        }
        public new PlayerControllerB MeetsStandardPlayerCollisionConditions(Collider other, bool inKillAnimation = false, bool overrideIsInsideFactoryCheck = false)
        {
            if (isEnemyDead)
            {
                return null;
            }
            if (!ventAnimationFinished)
            {
                return null;
            }
            if (inKillAnimation)
            {
                return null;
            }
            if (stunNormalizedTimer >= 0f)
            {
                return null;
            }
            PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
            if (component == null || component != GameNetworkManager.Instance.localPlayerController)
            {
                return null;
            }
            if (!PlayerIsTargetable2(component, overrideIsInsideFactoryCheck,false))
            {
                Debug.Log("Player is not targetable");
                return null;
            }
            return component;
        }

        public override void OnCollideWithPlayer(Collider other)
        {
            if (!other.gameObject.GetComponent<PlayerControllerB>())
            {
                return;
            }
            base.OnCollideWithPlayer(other);
            if (!inKillAnimation && !isEnemyDead && currentBehaviourStateIndex == 2)
            {
                PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
                if (playerControllerB != null)//check player is target, to stop him murdering random players when aggro
                {
                    inKillAnimation = true;
                    StartCoroutine(killPlayerAnimation((int)playerControllerB.playerClientId));
                    KillPlayerServerRpc((int)playerControllerB.playerClientId);
                }
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void KillPlayerServerRpc(int playerId)
        {
            ScopophobiaPlugin.Instance.LogInfoExtended($"Running Kill Player Server Rpc");
            KillPlayerClientRpc(playerId);
        }

        [ClientRpc]
        private void KillPlayerClientRpc(int playerId)
        {
            ScopophobiaPlugin.Instance.LogInfoExtended($"Running Kill Player Client Rpc. Current Target Count: {SCP096Targets.Count}");
            StartCoroutine(killPlayerAnimation(playerId));
        }

        private IEnumerator killPlayerAnimation(int playerId)
        {
            ScopophobiaPlugin.Instance.LogInfoExtended($"Running Kill Player Animation. Current Target Count: {SCP096Targets.Count}");
            inKillAnimation = true;
            PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[playerId];
            if (isOutside && transform.position.y < -80f)
            {
                SetEnemyOutside();
            }
            else if (!isOutside && transform.position.y > -80f)
            {
                SetEnemyOutside(outside: true);
            }
            int preAmount = SCP096Targets.Count;
            playerScript.KillPlayer(Vector3.zero, true, CauseOfDeath.Mauling, 0);
            if (CoronerProxy.Ready) CoronerProxy.SetDeathCause(playerId);
            AddTargetToList(playerId, remove: true);
            creatureSFX.PlayOneShot(killPlayerSFX);
            creatureAnimator.SetInteger("TargetsLeft", preAmount - 1);
            creatureAnimator.SetTrigger("kill");
            ScopophobiaPlugin.Instance.LogInfoExtended($"Running Kill Player Client Rpc Final. Current Target Count: {SCP096Targets.Count}");
            if (Config.deathMakesBloody && bloodyMaterial != null)
                ApplyBloodyMaterial();
            if (preAmount - 1 <= 0)
            {
                SitDown();
            }
            yield return new WaitForSeconds(1f);
            inKillAnimation = false;
        }
        private void ApplyBloodyMaterial()
        {
            var renderer = transform.Find("SCP096Model")?.Find("tsg_placeholder")?.GetComponent<SkinnedMeshRenderer>();

            renderer?.material = bloodyMaterial;
        }
        public void SitDown()
        {
            SwitchToBehaviourState(0);
            SitDownOnLocalClient();
            SitDownServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void SitDownServerRpc()
        {
            SitDownClientRpc();
        }

        [ClientRpc]
        private void SitDownClientRpc()
        {
            SitDownOnLocalClient();
        }

        public void SitDownOnLocalClient()
        {
            sitting = true;
            roamWaitTime = Random.Range(45f, 50f);
            creatureAnimator.SetBool("Rage", value: false);
            creatureAnimator.SetBool("Sitting", value: true);
        }

        public void AddTargetToList(int playerId, bool remove = false, string cause = "")
        {
            ScopophobiaPlugin.Instance.LogInfoExtended($"Running AddTargetToList. Current Target Count: {SCP096Targets.Count}, Cause: {cause}");
            PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[playerId];
            if (remove)
            {
                if (!SCP096Targets.Contains(playerScript))
                {
                    return;
                }
            }
            else if (SCP096Targets.Contains(playerScript))
            {
                return;
            }
            AddTargetToListServerRpc(playerId, remove);
            AddTargetToListOnLocalClient(playerId, remove);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddTargetToListServerRpc(int playerId, bool remove)
        {
            ScopophobiaPlugin.Instance.LogInfoExtended($"Running AddTarget Server Rpc. Current Target Count: {SCP096Targets.Count}");
            AddTargetToListClientRpc(playerId, remove);
        }

        [ClientRpc]
        public void AddTargetToListClientRpc(int playerId, bool remove)
        {
            ScopophobiaPlugin.Instance.LogInfoExtended($"Running AddTarget Client Rpc. Current Target Count: {SCP096Targets.Count}");
            AddTargetToListOnLocalClient(playerId, remove);
        }

        public void AddTargetToListOnLocalClient(int playerId, bool remove)
        {
            ScopophobiaPlugin.Instance.LogInfoExtended($"Running AddTargetOnLocalClient. Current Target Count: {SCP096Targets.Count}");
            PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[playerId];
            if (remove)
            {
                if (SCP096Targets.Contains(playerScript))
                {
                    SCP096Targets.Remove(playerScript);
                }
            }
            else if (!SCP096Targets.Contains(playerScript))
            {
                SCP096Targets.Add(playerScript);
            }
            ScopophobiaPlugin.Instance.LogInfoExtended($"Finishing up AddToListOnLocalClient. Current Target Count: {SCP096Targets.Count}");
        }

        private void BeginPryOpenDoor()
        {
            StartPryOpenDoorAnimationOnLocalClient();
            PryOpenDoorServerRpc((int)GameNetworkManager.Instance.localPlayerController.actualClientId);
        }

        private void FinishPryOpenDoor(bool cancelledEarly)
        {
            FinishPryOpenDoorAnimationOnLocalClient(cancelledEarly);
            PryOpenDoorServerRpc((int)GameNetworkManager.Instance.localPlayerController.actualClientId, finishAnim: true, cancelledEarly);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PryOpenDoorServerRpc(int playerWhoSent, bool finishAnim = false, bool cancelledEarly = false)
        {
            PryOpenDoorClientRpc(playerWhoSent, finishAnim, cancelledEarly);
        }

        [ClientRpc]
        public void PryOpenDoorClientRpc(int playerWhoSent, bool finishAnim = false, bool cancelledEarly = false)
        {
            if (!finishAnim)
            {
                StartPryOpenDoorAnimationOnLocalClient();
            }
            else
            {
                FinishPryOpenDoorAnimationOnLocalClient(cancelledEarly);
            }
        }

        private void FinishPryOpenDoorAnimationOnLocalClient(bool cancelledEarly = false)
        {
            if (!cancelledEarly)
            {
                shipDoor.shipDoorsAnimator.SetBool("Closed", value: false);
                StartOfRound.Instance.SetShipDoorsClosed(closed: false);
                StartOfRound.Instance.SetShipDoorsOverheatLocalClient();
                shipDoor.doorPower = 0f;
            }
            pryingOpenDoor = false;
            inSpecialAnimation = false;
            creatureAnimator.SetBool("PryingOpenDoor", value: false);
            shipDoor.shipDoorsAnimator.SetBool("PryingOpenDoor", value: false);
            creatureAnimator.SetLayerWeight(0, 1f);
        }

        private void StartPryOpenDoorAnimationOnLocalClient()
        {
            agent.enabled = false;
            pryingOpenDoor = true;
            inSpecialAnimation = true;
            creatureAnimator.SetBool("PryingOpenDoor", value: true);
            shipDoor.shipDoorsAnimator.SetBool("PryingOpenDoor", value: true);
            shipDoor.shipDoorsAnimator.SetFloat("pryOpenDoor", 0f);
            breakDownDoorAudio.PlayOneShot(breakAndEnter);
            WalkieTalkie.TransmitOneShotAudio(breakDownDoorAudio, breakAndEnter);
            RoundManager.Instance.PlayAudibleNoise(transform.position, 15f, 0.9f);
            StartOfRound.Instance.speakerAudioSource.PlayOneShot(shipAlarm);
            WalkieTalkie.TransmitOneShotAudio(StartOfRound.Instance.speakerAudioSource, shipAlarm);
            if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, transform.position) < 18f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            }
        }
        public bool BreakIntoShip()
        {
            if (!canBreakIntoShip || shipDoor == null) return false;
            if (pryingOpenDoor)
            {
                if (pryingDoorAnimTime >= 1f)
                {
                    FinishPryOpenDoor(cancelledEarly: false);
                }
                return true;
            }
            if (!CanStartPrying()) return false;
            BeginPryOpenDoor();
            return true;
        }
        private bool CanStartPrying()
        {
            return StartOfRound.Instance.hangarDoorsClosed && IsDestinationInShip() && IsNearShipDoor();
        }

        private bool IsDestinationInShip()
        {
            return StartOfRound.Instance.shipStrictInnerRoomBounds.bounds.Contains(destination);
        }

        private bool IsNearShipDoor()
        {
            return Vector3.Distance(transform.position, shipDoor.outsideDoorPoint.position) < 4f;
        }
        private void SetShyGuyInitialValues()
        {
            mainCollider = gameObject.GetComponentInChildren<Collider>();
            farAudio = transform.Find("FarAudio").GetComponent<AudioSource>();
            creatureVoice = transform.Find("CreatureVoice").GetComponent<AudioSource>();
            targetPlayer = null;
            inKillAnimation = false;
            pryingOpenDoor = false;
            SCP096Targets.Clear();
            creatureAnimator.SetFloat("VelocityX", 0f);
            creatureAnimator.SetFloat("VelocityZ", 0f);
            creatureAnimator.SetFloat("DistanceToTarget", 999f);
            creatureAnimator.SetFloat("pryOpenDoor", 999f);
            creatureAnimator.SetInteger("SitActionTimer", 0);
            creatureAnimator.SetInteger("TargetsLeft", 0);
            creatureAnimator.SetBool("Rage", value: false);
            creatureAnimator.SetBool("Sitting", value: false);
            creatureAnimator.SetBool("triggered", value: false);
            creatureAnimator.SetBool("PryingOpenDoor", value: false);
            mainCollider.isTrigger = true;
            farAudio.volume = 0f;
            farAudio.Stop();
            creatureVoice.Stop();
        }
    }
}