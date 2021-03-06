﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{

    [SerializeField] private AudioClip[] clips;
    private int clipIndex;
    private AudioSource audio;
    private bool audioPlaying = false;

    void Start()
    {
        audio = gameObject.GetComponent<AudioSource>();
    }
    void Update()
    {

        if (!audio.isPlaying)
        {

            clipIndex = Random.Range(0, clips.Length);
            audio.clip = clips[clipIndex];
            audio.PlayDelayed(Random.Range(10f, 20f));
            Debug.Log("Nothing playing, set new audio to " + audio.clip.name);
        }
    }
}
