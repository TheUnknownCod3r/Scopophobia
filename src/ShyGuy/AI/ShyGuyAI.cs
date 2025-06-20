﻿using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using BepInEx.Logging;
using DunGen;
using GameNetcodeStuff;
using Scopophobia;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Zeekerss.Core.Singletons;

namespace ShyGuy.AI
{
    public class ShyGuyAI : EnemyAI
    {
        private Transform localPlayerCamera;

        private Vector3 mainEntrancePosition;

        public Collider mainCollider;

        public VehicleController CompanyCruiser;

        public HangarShipDoor shipDoors;

        public static bool pryingOpenDoors;

        public AudioClip shipAlarm;

        public AudioSource breakDownDoorAudio;

        public AudioClip breakAndEnter;


        public AudioSource farAudio;
        public string typeName = "ShyGuyAI";
        float preTime = Time.deltaTime;
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

        public Material bloodyMaterial;

        public AISearchRoutine roamMap;

        public Transform shyGuyFace;

        private Vector3 spawnPosition;

        private Vector3 previousPosition;

        private int previousState = -1;

        private float roamWaitTime = 40f;

        private bool roamShouldSit;

        private bool sitting;

        private float lastRunSpeed;

        private float seeFaceTime;

        private float triggerTime;

        private float triggerDuration = 66.4f;

        private float timeToTrigger = 0.5f;

        private float lastInterval = Time.realtimeSinceStartup;

        private bool inKillAnimation;

        private bool isInElevatorStartRoom;

        private float timeAtLastUsingEntrance;

        private MineshaftElevatorController elevatorScript;

        public List<PlayerControllerB> SCP096Targets = new List<PlayerControllerB>();

        public override void Start()
        {
            base.Start();
            CompanyCruiser = UnityEngine.Object.FindObjectOfType<VehicleController>();
            shipDoors = UnityEngine.Object.FindObjectOfType<HangarShipDoor>();
            triggerDuration = Config.triggerTime;
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
                Transform model = transform.Find("SCP096Model");
                if (model != null)
                {
                    Transform modelMesh = model.Find("tsg_placeholder");
                    if (modelMesh != null)
                    {
                        SkinnedMeshRenderer skinnedModel = modelMesh.GetComponent<SkinnedMeshRenderer>();
                        if (skinnedModel != null)
                        {
                            skinnedModel.material = bloodyMaterial;
                        }
                    }
                }
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
            mainEntrancePosition = RoundManager.Instance.GetNavMeshPosition(RoundManager.FindMainEntrancePosition(getTeleportPosition: true, isOutside));
            if (isOutside)
            {
                if (allAINodes == null || allAINodes.Length == 0)
                {
                    allAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
                }
                if (GameNetworkManager.Instance.localPlayerController != null)
                {
                    EnableEnemyMesh(!StartOfRound.Instance.hangarDoorsClosed || !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom);
                }
            }
            else if (allAINodes == null || allAINodes.Length == 0)
            {
                allAINodes = GameObject.FindGameObjectsWithTag("AINode");
            }
            path1 = new NavMeshPath();
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
                        if (stunNormalizedTimer > 0f)
                        {
                            agent.speed = 0f;
                        }
                        else if (sitting)
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
                        if (roamWaitTime <= 20f && roamMap.inProgress && base.targetPlayer == null)
                        {
                            StopSearch(roamMap);
                            lastInterval = Time.realtimeSinceStartup;
                        }
                        else if (roamWaitTime > 2.5f && roamWaitTime <= 15f && !roamMap.inProgress && base.targetPlayer == null && roamShouldSit)
                        {
                            creatureVoice.Stop();
                            sitting = true;
                            creatureAnimator.SetBool("Sitting", value: true);
                            creatureVoice.GetComponent<AudioSource>().volume = 0.5f;
                            creatureVoice.GetComponent<AudioSource>().clip = crySittingSFX;
                            creatureVoice.GetComponent<AudioSource>().Play();
                            creatureVoice.GetComponent<AudioSource>().time = preTime;
                            lastInterval = Time.realtimeSinceStartup;
                        }
                        else if (!(base.targetPlayer != null) && base.targetPlayer == null && !roamMap.inProgress && roamWaitTime <= 0f)
                        {
                            if (!sitting)
                            {
                                roamShouldSit = Random.Range(1, 5) == 1;
                                roamWaitTime = Random.Range(25f, 32.5f);
                                StartSearch(spawnPosition, roamMap);
                                lastInterval = Time.realtimeSinceStartup;
                                break;
                            }
                            creatureVoice.Stop();
                            sitting = false;
                            roamShouldSit = false;
                            roamWaitTime = Random.Range(21f, 25f);
                            creatureAnimator.SetBool("Sitting", value: false);
                            creatureVoice.GetComponent<AudioSource>().volume = 0.5f;
                            creatureVoice.GetComponent<AudioSource>().clip = crySFX;
                            creatureVoice.GetComponent<AudioSource>().Play();
                            creatureVoice.GetComponent<AudioSource>().time = preTime;
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
                        PlayerControllerB oldTargetPlayer = targetPlayer;
                        float closestDist = float.PositiveInfinity;
                        if (!StartOfRound.Instance.shipIsLeaving)//Fix Index Out of Bounds Reported by NPC, Caused by Shy guy array modification when leaving planet
                        {
                            foreach (PlayerControllerB hunted in SCP096Targets.ToList())
                            {
                                bool sameArea = hunted.isInsideFactory == !isOutside;
                                bool allowedToLeave = true;
                                if (!Config.canExitFacility && !sameArea)
                                {
                                    allowedToLeave = false;
                                }
                                if (!hunted.isPlayerDead && allowedToLeave)
                                {
                                    if (PlayerIsTargetable(hunted, cannotBeInShip: false, overrideInsideFactoryCheck: true) && Vector3.Distance(hunted.transform.position, transform.position) < closestDist)
                                    {
                                        closestDist = Vector3.Magnitude(hunted.transform.position - transform.position);
                                        targetPlayer = hunted;
                                    }
                                }
                                else
                                {
                                    AddTargetToList((int)hunted.actualClientId, remove: true);
                                }
                            }
                        }
                        if (targetPlayer != null)
                        {
                            bool flag = false;
                            creatureAnimator.SetFloat("DistanceToTarget", Vector3.Distance(transform.position, targetPlayer.transform.position));
                            if (roamMap.inProgress)
                            {
                                StopSearch(roamMap);
                            }
                            if (targetPlayer != oldTargetPlayer)
                            {
                                ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
                            }
                            if(!isOutside && RoundManager.Instance.currentDungeonType == 4)//Checks if Shy guy is Outside, if not, will run the code in the brackets.
                            {
                                elevatorScript = UnityEngine.Object.FindObjectOfType<MineshaftElevatorController>();//Lets see if the Elevator Controls exist, I hope they do, we're on the mineshaft
                                if (elevatorScript != null)
                                {
                                    //ScopophobiaPlugin.logger.LogInfo("Starting Elevator Checks");
                                    if (isInElevatorStartRoom)
                                    {
                                        if (Vector3.Distance(base.transform.position, elevatorScript.elevatorBottomPoint.position) < 3f)//Check distance from Bottom Button
                                        {
                                            isInElevatorStartRoom = false;
                                            //ScopophobiaPlugin.logger.LogInfo("Shy guy is at Lower Elevator");
                                        }
                                    }
                                    else if (Vector3.Distance(base.transform.position, elevatorScript.elevatorTopPoint.position) < 3f)//Another Distance check, this time for Top Button.
                                    {
                                        isInElevatorStartRoom = true;
                                        //ScopophobiaPlugin.logger.LogInfo("Shy guy is at Upper Elevator");
                                    }
                                    if (RoundManager.Instance.currentDungeonType == 4)
                                    {
                                        //ScopophobiaPlugin.logger.LogInfo("Map Interior is Mineshaft, Committing to Elevator Checks");
                                        if (!isInElevatorStartRoom)//if not in the Elevator Start Room, Need to call the Elevator
                                        {
                                            UseElevator(true);
                                            //ScopophobiaPlugin.logger.LogInfo("Shy guy Going Up");
                                            return;
                                        }
                                        else
                                        {
                                            if (!targetPlayer.isPlayerDead && targetPlayer.isPlayerControlled && targetPlayer.isInsideFactory && Vector3.Distance(targetPlayer.transform.position, elevatorScript.elevatorTopPoint.position) > 50f)//Is the player Inside, or hiding, Lets Distance check them from the Top Point
                                            {
                                                UseElevator(false);//Player is actually inside, Probably a fire exit. Lets go back down
                                                //ScopophobiaPlugin.logger.LogInfo("Flag 3 set, Shy Guy Going Down");
                                                return;
                                            }
                                            else//Player is outside, lets just continue
                                            {
                                                //ScopophobiaPlugin.logger.LogInfo("Everything Executed Correctly here. No flags needed");
                                            }
                                        }
                                    }
                                }
                            }
                            if (targetPlayer.isInsideFactory != !isOutside)//Is Target inside or outside?
                            {
                                if (Vector3.Distance(transform.position, mainEntrancePosition) < 2f)//am I at the door?
                                {
                                    TeleportEnemy(RoundManager.FindMainEntrancePosition(getTeleportPosition: true, !isOutside), !isOutside);
                                    agent.speed = 0f;
                                    return;
                                }
                                else//Im not at the door, but lets move towards it since im in the wrong place.
                                {
                                    movingTowardsTargetPlayer = false;
                                    SetDestinationToPosition(mainEntrancePosition);
                                }
                            }
                            if (BreakIntoShip())
                            {
                                break;
                            }
                            else//Player in sights, continuing attack strategy
                            {
                                SetMovingTowardsTargetPlayer(targetPlayer);
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

        public void TeleportEnemy(Vector3 pos, bool setOutside)
        {
            Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(pos);
            if (IsOwner)
            {
                agent.enabled = false;
                transform.position = navMeshPosition;
                agent.enabled = true;
            }
            else
            {
                transform.position = navMeshPosition;
            }
            serverPosition = navMeshPosition;
            SetEnemyOutside(setOutside);
            EntranceTeleport entranceTeleport = RoundManager.FindMainEntranceScript(setOutside);
            if (entranceTeleport.doorAudios != null && entranceTeleport.doorAudios.Length != 0)
            {
                entranceTeleport.entrancePointAudio.PlayOneShot(entranceTeleport.doorAudios[0]);
                WalkieTalkie.TransmitOneShotAudio(entranceTeleport.entrancePointAudio, entranceTeleport.doorAudios[0]);
            }
        }
        private bool UseElevator(bool goUp)
        {
            Vector3 vector = ((!goUp) ? elevatorScript.elevatorTopPoint.position : elevatorScript.elevatorBottomPoint.position);
            if (elevatorScript.elevatorFinishedMoving && !PathIsIntersectedByLineOfSight(elevatorScript.elevatorInsidePoint.position, calculatePathDistance: false, avoidLineOfSight: false))
            {
                if (elevatorScript.elevatorDoorOpen && Vector3.Distance(base.transform.position, elevatorScript.elevatorInsidePoint.position) < 1f && elevatorScript.elevatorMovingDown == goUp)
                {
                    elevatorScript.PressElevatorButtonOnServer(requireFinishedMoving: true);
                }
                SetDestinationToPosition(elevatorScript.elevatorInsidePoint.position);
                return true;
            }
            if (Vector3.Distance(base.transform.position, elevatorScript.elevatorInsidePoint.position) > 1f && !PathIsIntersectedByLineOfSight(vector, calculatePathDistance: false, avoidLineOfSight: false))
            {
                if (elevatorScript.elevatorDoorOpen && Vector3.Distance(base.transform.position, vector) < 1f && elevatorScript.elevatorMovingDown != goUp && !elevatorScript.elevatorCalled)
                {
                    elevatorScript.CallElevatorOnServer(goUp);
                }
                SetDestinationToPosition(vector);
                return true;
            }
            return false;
        }

        public override void Update()
        {
            if (isEnemyDead || GameNetworkManager.Instance == null)
            {
                return;
            }
            CalculateAnimationSpeed();
            bool canSeeFace = GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(shyGuyFace.position, Config.faceTriggerRange, 45);
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
                        AddTargetToList((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
                    }
                    if (currentBehaviourStateIndex == 0)
                    {
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
                    if (pryingOpenDoors)
                    {
                        base.transform.position = Vector3.Lerp(base.transform.position, shipDoors.outsideDoorPoint.position, 7f * Time.deltaTime);
                        base.transform.rotation = Quaternion.Lerp(base.transform.rotation, shipDoors.outsideDoorPoint.rotation, 7f * Time.deltaTime);
                        shipDoors.shipDoorsAnimator.SetFloat("pryOpenDoor", 0.1f);
                        creatureAnimator.SetLayerWeight(1, Mathf.Max(0f, creatureAnimator.GetLayerWeight(1) - Time.deltaTime * 5f));
                        BreakIntoShip();
                        return;
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
                        farAudio.GetComponent<AudioSource>().volume = 0f;
                        creatureVoice.GetComponent<AudioSource>().volume = 0.5f;
                        creatureVoice.GetComponent<AudioSource>().clip = crySFX;
                        creatureVoice.GetComponent<AudioSource>().Play();
                        creatureVoice.GetComponent<AudioSource>().time = preTime;
                    }
                    if (!creatureVoice.isPlaying)
                    {
                        creatureVoice.GetComponent<AudioSource>().volume = 0.5f;
                        creatureVoice.GetComponent<AudioSource>().clip = crySFX;
                        creatureVoice.GetComponent<AudioSource>().Play();
                        creatureVoice.GetComponent<AudioSource>().time = preTime;
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
                        creatureVoice.Stop();
                        farAudio.volume = 0.275f;
                        farAudio.clip = panicSFX;
                        farAudio.Play();
                        agent.speed = 0f; 
                        farAudio.time = preTime;

                        triggerTime = triggerDuration;
                    }
                    triggerTime -= Time.deltaTime;
                    if (triggerTime <= 0f)
                    {
                        SwitchToBehaviourState(2);
                    }
                    break;
                case 2:
                    mainCollider.isTrigger = true;
                    if (previousState != 2)
                    {
                        mainCollider.isTrigger = true;
                        previousState = 2;
                        creatureAnimator.SetBool("Rage", value: true);
                        creatureAnimator.SetBool("triggered", value: false);
                        farAudio.Stop();
                        farAudio.GetComponent<AudioSource>().volume = 0.4f;
                        farAudio.GetComponent<AudioSource>().clip = screamSFX;
                        farAudio.GetComponent<AudioSource>().Play();
                        farAudio.GetComponent<AudioSource>().time = preTime;//read all audio as components
                    }
                    break;
            }
            base.Update();
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
                if (playerControllerB != null)
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
            KillPlayerClientRpc(playerId);
        }

        [ClientRpc]
        private void KillPlayerClientRpc(int playerId)
        {
            StartCoroutine(killPlayerAnimation(playerId));
        }

        private IEnumerator killPlayerAnimation(int playerId)
        {
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
            playerScript.KillPlayer(playerScript.transform.position, spawnBody: true, CauseOfDeath.Mauling, 1);
            AddTargetToList(playerId, remove: true);
            creatureSFX.clip = killPlayerSFX;
            creatureSFX.Play();
            creatureAnimator.SetInteger("TargetsLeft", preAmount - 1);
            creatureAnimator.SetTrigger("kill");
            if (preAmount - 1 <= 0)
            {
                SitDown();
            }
            if (Config.deathMakesBloody && bloodyMaterial != null)
            {
                Transform model = transform.Find("SCP096Model");
                if (model != null)
                {
                    Transform modelMesh = model.Find("tsg_placeholder");
                    if (modelMesh != null)
                    {
                        SkinnedMeshRenderer skinnedModel = modelMesh.GetComponent<SkinnedMeshRenderer>();
                        if (skinnedModel != null)
                        {
                            skinnedModel.material = bloodyMaterial;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(1f);
            inKillAnimation = false;
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

        public void AddTargetToList(int playerId, bool remove = false)
        {
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
            AddTargetToListOnLocalClient(playerId, remove);
            AddTargetToListServerRpc(playerId, remove);
        }

        [ServerRpc(RequireOwnership = false)]
        public void AddTargetToListServerRpc(int playerId, bool remove)
        {
            AddTargetToListClientRpc(playerId, remove);
        }

        [ClientRpc]
        public void AddTargetToListClientRpc(int playerId, bool remove)
        {
            AddTargetToListOnLocalClient(playerId, remove);
        }

        public void AddTargetToListOnLocalClient(int playerId, bool remove)
        {
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
        }

        public void BeginPryOpenDoor()
        {
            StartPryOpenDoorAnimationOnLocalClient();
            PryOpenDoorServerRpc((int)GameNetworkManager.Instance.localPlayerController.actualClientId, true, true);
        }

        public void FinishPryOpenDoor(bool endEarly)
        {
            FinishPryOpenDoorAnimationOnLocalClient(endEarly);
            PryOpenDoorServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId, finishAnim: true, endEarly);
        }
        [ServerRpc(RequireOwnership = false)]
        public void PryOpenDoorServerRpc(int clientSent, bool finishAnim = false, bool endEarly = false)
        {
            PryOpenDoorClientRpc(clientSent, finishAnim, endEarly);
        }
        [ClientRpc]
        public void PryOpenDoorClientRpc(int clientSent, bool finishAnim = false, bool endEarly = false)
        {
            FinishPryOpenDoorAnimationOnLocalClient(endEarly);
        }
        private void FinishPryOpenDoorAnimationOnLocalClient(bool cancelledEarly = false)
        {
            shipDoors.shipDoorsAnimator.SetBool("Closed", value: false);
            StartOfRound.Instance.SetDoorsClosedServerRpc(false);
            StartOfRound.Instance.SetShipDoorsOverheatServerRpc();
            shipDoors.doorPower = 0f;
        }

        private void StartPryOpenDoorAnimationOnLocalClient()
        {
            if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.transform.position) < 15f)
            {
                HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            }
        }

        public bool BreakIntoShip()
        {
            if (shipDoors == null)
            {
                Debug.LogError("Shy Guy error: ship door is null");
                return false;
            }
            if (pryingOpenDoors)
            {
                return true;
            }
            if (StartOfRound.Instance.hangarDoorsClosed && StartOfRound.Instance.shipStrictInnerRoomBounds.bounds.Contains(targetPlayer.transform.position) && Vector3.Distance(base.transform.position, shipDoors.outsideDoorPoint.position) < 4f)
            {
                BeginPryOpenDoor();
                return true;
            }
            return false;
        }
        private void SetShyGuyInitialValues()
        {
            mainCollider = gameObject.GetComponentInChildren<Collider>();
            farAudio = transform.Find("FarAudio").GetComponent<AudioSource>();
            targetPlayer = null;
            inKillAnimation = false;
            SCP096Targets.Clear();
            creatureAnimator.SetFloat("VelocityX", 0f);
            creatureAnimator.SetFloat("VelocityZ", 0f);
            creatureAnimator.SetFloat("DistanceToTarget", 999f);
            creatureAnimator.SetInteger("SitActionTimer", 0);
            creatureAnimator.SetInteger("TargetsLeft", 0);
            creatureAnimator.SetBool("Rage", value: false);
            creatureAnimator.SetBool("Sitting", value: false);
            creatureAnimator.SetBool("triggered", value: false);
            mainCollider.isTrigger = true;
            farAudio.volume = 0f;
            farAudio.Stop();
            creatureVoice.Stop();
        }
    }
}