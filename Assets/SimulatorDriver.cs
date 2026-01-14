using UnityEngine;
using System;

public class SimulatorDriver : MonoBehaviour
{
    [SerializeField] private Simulator simulator;

    private void Awake()
    {
        simulator ??= new Simulator();
    }

    private void Update()
    {
        // Drive the discrete-event simulation forward each frame.
        simulator.Run(TimeSpan.FromSeconds(Time.deltaTime));
    }
}
