using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Code
{
    public class ConnectedWaypoint : Waypoint
    {
        [SerializeField]
        protected float _connectivityRadius = 50f; //connection radius

        List<ConnectedWaypoint> _connections; //waypoint list

        // Start is called before the first frame update
        public void Start()
        {
            GameObject[] allWaypoints = GameObject.FindGameObjectsWithTag("Waypoint"); //get all waypoint objects in scene

            _connections = new List<ConnectedWaypoint>(); //create list of waypoint refs

            for (int i = 0; i < allWaypoints.Length; i++)
            {
                ConnectedWaypoint nextWaypoint = allWaypoints[i].GetComponent<ConnectedWaypoint>(); //check for connected waypoint

                if (nextWaypoint != null) //if found + not null
                {
                    if (Vector3.Distance(this.transform.position, nextWaypoint.transform.position) <= _connectivityRadius && nextWaypoint != this) //if distance between current + next waypoint, + does not equal this
                    {
                        _connections.Add(nextWaypoint); //add to current position
                    }
                }
            }
        }

        public override void OnDrawGizmos()
        {
            Gizmos.color = Color.red; //set gizmo colour
            Gizmos.DrawWireSphere(transform.position, debugDrawRadius); //set gizmos

            Gizmos.color = Color.yellow; //set connectivity radius colour
            Gizmos.DrawWireSphere(transform.position, _connectivityRadius); //set connectivity radius
        }

        public ConnectedWaypoint NextWaypoint(ConnectedWaypoint previousWaypoint)
        {
            if (_connections.Count == 0) //if 0 connections
            {
                Debug.LogError("insufficient waypoint count"); //show msg
                return null; //return
            }
            else if (_connections.Count == 1 && _connections.Contains(previousWaypoint)) //if there's only 1 waypoint + it's the previous one
            {
                return previousWaypoint; //use previous
            }
            else //find random one that isn't the previous one
            {
                ConnectedWaypoint nextWaypoint; //set to next waypoint
                int nextIndex = 0;

                do
                {
                    nextIndex = UnityEngine.Random.Range(0, _connections.Count); //randomise
                    nextWaypoint = _connections[nextIndex]; //add to index
                }
                while (nextWaypoint == previousWaypoint);

                return nextWaypoint; //return
            }
        }
    }
}