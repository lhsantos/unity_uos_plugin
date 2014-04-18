using System.Collections.Generic;
using UnityEngine;
using UOS;

[RequireComponent(typeof(uOS))]
public class TestUOS : MonoBehaviour
{
    /// <summary>
    /// Called right before the first update.
    /// </summary>
    void Start()
    {
        uOS.Init();
    }

    /// <summary>
    /// Called once every frame.
    /// </summary>
    void Update()
    {
    }
}
