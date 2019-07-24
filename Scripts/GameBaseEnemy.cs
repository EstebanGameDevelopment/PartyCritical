using UnityEngine;
using UnityEngine.UI;
using YourCommonTools;
using YourNetworkingTools;

namespace PartyCritical
{
    /******************************************
     * 
     * GameEnemy
     * 
     * Logic of the enemy
     * 
     * @author Esteban Gallardo
     */
    public class GameBaseEnemy : ActorTimeline
    {
        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        protected GameObject m_target;
        protected string m_prefabEnemyName;

        // PATHFINDING
        protected float m_timeOutRecalculate = 0;
        private bool m_targetReacheable = false;
        private bool m_precalculatedWaypointHasBeenReached = true;
        private Vector3 m_originLastFreeCell = Vector3.down;
        private Vector3 m_targetLastFreeCell = Vector3.down;

        private float m_timeoutToKinematic = 0.5f;

        // ----------------------------------------------
        // GETTERS/SETTERS
        // ----------------------------------------------	
        public virtual GameObject CameraControllerObject
        {
            get { return null; }
        }
        public virtual bool UserRaycastFiltering
        {
            get { return false; }
        }
        public virtual float TIMEOUT_RECALCULATE_POSITION
        {
            get { return 4; }
        }

        // -------------------------------------------
        /* 
		* InitializeCommon
		*/
        public override void InitializeCommon()
        {
            m_speed = 3;
            m_timeOutRecalculate = TIMEOUT_RECALCULATE_POSITION;
        }

        // -------------------------------------------
        /* 
		* Manager of global events
		*/
        protected override void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
        {
            base.OnNetworkEvent(_nameEvent, _isLocalEvent, _networkOriginID, _networkTargetID, _list);
        }

        // -------------------------------------------
        /* 
		* Manager of global events
		*/
        protected override void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == EVENT_ACTORTIMELINE_GO_ACTION_ENDED)
            {
                if (NameActor == (string)_list[0])
                {
                    m_timeOutRecalculate = TIMEOUT_RECALCULATE_POSITION + 1;
                    m_precalculatedWaypointHasBeenReached = true;
                    if (_list.Length > 1)
                    {
                        if (_list[1] is Vector3)
                        {
                            m_originLastFreeCell = (Vector3)_list[1];
                        }                        
                    }
                }
            }
        }

        // -------------------------------------------
        /* 
        * GoToPosition
        */
        public override bool GoToPosition(Vector3 _origin, Vector3 _target, float _speed, int _limitSearch, params string[] _masksToIgnore)
        {
            m_goToPosition = true;
            m_speed = _speed;
            m_targetPath = Utilities.Clone(_target);
            Utilities.Clone(ref m_masksToIgnore, _masksToIgnore);
            bool checkBlockedPath = false;
            if (Vector3.Distance(this.transform.position, m_target.transform.position) <= PathFindingController.Instance.GetCellSize() * 2)
            {
                GameObject collidedObject = Utilities.GetCollidedObjectByRayTarget(m_target.transform.position, this.transform.position);
                if (collidedObject != null)
                {
                    if (collidedObject == CameraControllerObject)
                    {
                        m_currentPathWaypoint = -1;
                        return true;
                    }
                }
            }
            if ((!checkBlockedPath && PathFindingController.Instance.CheckBlockedPath(_origin, _target, 3, m_masksToIgnore))
                || (checkBlockedPath && PathFindingController.Instance.CheckBlockedPath(this.transform.position, m_target.transform.position, 3, m_masksToIgnore)))
            {
                m_currentPathWaypoint = 0;
                CalculateInternalPathWaypoints(_origin, _target, _limitSearch, true, UserRaycastFiltering);
                if (m_pathWaypoints != null)
                {
                    return (m_pathWaypoints.Count != 0);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                m_currentPathWaypoint = -1;
                return true;
            }
        }

        // -------------------------------------------
        /* 
		* ShouldRunLogic
		*/
        public bool ShouldRunLogic()
        {
            return (m_animation == ANIMATION_IDLE) || (m_animation == ANIMATION_WALK);
        }

        // -------------------------------------------
        /* 
		* Logic
		*/
        public override void Logic()
        {
            if (IsMine())
            {
                if (ShouldRunLogic())
                {
                    if (m_timeoutToKinematic != -1)
                    {
                        m_timeoutToKinematic -= Time.deltaTime;
                        if (m_timeoutToKinematic <= 0)
                        {
                            m_timeoutToKinematic = -1;
                            this.transform.GetComponent<Rigidbody>().isKinematic = true;
                            this.transform.GetComponent<Rigidbody>().useGravity = false;
                            this.transform.GetComponent<BoxCollider>().isTrigger = true;
                            NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECHARACTER_NEW_ANIMATION, NetworkID.NetID.ToString(), NetworkID.UID.ToString(), m_animation.ToString(), true.ToString());
                        }
                    }

                    if (m_target != null)
                    {
                        // SET LAST FREE CELL FOR ORIGIN
                        Vector3 originLastFreeCell = PathFindingController.Instance.GetClosestFreeNode(this.transform.position);
                        if (originLastFreeCell != Vector3.down)
                        {
                            m_originLastFreeCell = originLastFreeCell;
                        }
                        if (m_originLastFreeCell == Vector3.down)
                        {
                            m_originLastFreeCell = PathFindingController.Instance.GetClosestFreeNode(this.transform.position);
                        }

                        // SET LAST FREE CELL FOR TARGET
                        Vector3 targetLastFreeCell = PathFindingController.Instance.GetClosestFreeNode(m_target.transform.position);
                        if (targetLastFreeCell != Vector3.down)
                        {
                            m_targetLastFreeCell = targetLastFreeCell;
                        }
                        if (m_targetLastFreeCell == Vector3.down)
                        {
                            m_targetLastFreeCell = PathFindingController.Instance.GetClosestFreeNode(m_target.transform.position);
                        }

                        m_timeOutRecalculate += Time.deltaTime;
                        if (m_timeOutRecalculate > TIMEOUT_RECALCULATE_POSITION)
                        {
                            m_timeOutRecalculate = 0;
                            if (!PathFindingController.Instance.IsPrecalculated)
                            {
                                m_precalculatedWaypointHasBeenReached = true;
                            }
                            else
                            {
                                if (Utilities.DistanceSqrtXZ(this.transform.position, m_target.transform.position) <= PathFindingController.Instance.GetCellSize() * 2)
                                {
                                    m_originLastFreeCell = this.transform.position;
                                    m_targetLastFreeCell = m_target.transform.position;
                                    m_precalculatedWaypointHasBeenReached = true;
                                }
                            }

                            if (m_precalculatedWaypointHasBeenReached)
                            {
                                m_timeOutRecalculate = 0;
                                // GameObject dotOrigin = PathFindingController.Instance.CreateSingleDot(m_originLastFreeCell, 3.5f, 1);
                                // GameObject.Destroy(dotOrigin, 3);
                                // GameObject dotTarget = PathFindingController.Instance.CreateSingleDot(m_targetLastFreeCell, 3.5f, 2);
                                // GameObject.Destroy(dotTarget, 3);
                                m_targetReacheable = GoToPosition(m_originLastFreeCell, m_targetLastFreeCell, m_speed, -1, new string[3] { LAYER_PLAYERS, LAYER_ENEMIES, LAYER_NPCS });
                                if (PathFindingController.Instance.IsPrecalculated)
                                {
                                    m_precalculatedWaypointHasBeenReached = false;
                                }
                                if (m_targetReacheable)
                                {
                                    ChangeAnimation(ANIMATION_WALK, true);
                                }
                                else
                                {
                                    ChangeAnimation(ANIMATION_IDLE, true);
                                }
                            }
                        }

                        if (m_targetReacheable)
                        {
                            if (m_targetPath != Vector3.zero)
                            {
                                GoToTargetPosition(true);
                            }
                            else
                            {
                                ChangeAnimation(ANIMATION_IDLE, true);
                            }
                        }
                    }
                }
            }
        }
    }
}