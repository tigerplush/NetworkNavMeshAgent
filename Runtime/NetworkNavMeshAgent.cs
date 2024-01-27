using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class NetworkNavMeshAgent : NetworkBehaviour
{
    private NavMeshPath _path;
    public NavMeshPath path {
        get
        {
            return _path;
        }
        set
        {
            if(value == null)
            {
                throw new NullReferenceException();
            }
            _path = value;
        }
    }

    private void Awake()
    {
        _path = new NavMeshPath();
    }

    [Networked]
    public Vector3 NetworkedPreviousPosition { get; set; }

    [SerializeField]
    private Vector3 _previousPosition;

    public Vector3 velocity
    {
        get
        {
            return transform.position - _previousPosition;
        }
    }

    [SerializeField]
    private int _currentCorner = -1;
    [SerializeField]
    private float _currentTime = 0f;

    public void SetDestination(Vector3 target)
    {
        SetDestinationRpc(target);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void SetDestinationRpc(Vector3 target)
    {
        path.ClearCorners();
        CalculatePath(transform.position, target, NavMesh.AllAreas, path);
    }

    private bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path)
    {
        if(NavMesh.CalculatePath(sourcePosition, targetPosition, areaMask, path))
        {
            _currentCorner = 0;
            _currentTime = 0f;
            return true;
        }
        return false;
    }

    public override void FixedUpdateNetwork()
    {
        if(_currentCorner > -1 && _currentCorner + 1 < _path.corners.Length)
        {
            // we have a path
            Vector3 startPosition = _path.corners[_currentCorner];
            Vector3 targetPosition = _path.corners[_currentCorner + 1];
            _previousPosition = transform.position;
            NetworkedPreviousPosition = transform.position;
            _currentTime += Runner.DeltaTime;
            Vector3 currentPosition = Vector3.Lerp(startPosition, targetPosition, _currentTime);
            transform.position = currentPosition;

            if((targetPosition - currentPosition).magnitude < Mathf.Epsilon)
            {
                _previousPosition = currentPosition;
                NetworkedPreviousPosition = currentPosition;
                // we're at the end of the path
                _currentTime = 0f;
                _currentCorner += 1;
                if(_currentCorner == path.corners.Length - 1)
                {
                    _currentCorner = -1;
                }
            }
        }
        if (velocity.sqrMagnitude > Mathf.Epsilon)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized);
        }
    }
}
