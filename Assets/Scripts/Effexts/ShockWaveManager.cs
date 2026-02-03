using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShockWaveManager : MonoBehaviour
{
	private static ShockWaveManager _instance;
	public static ShockWaveManager Instance
	{
		get
		{
			if(_instance == null)
			{
				_instance = FindObjectOfType<ShockWaveManager>();
			}
			return _instance;
		}
	}

	[SerializeField] private float _shockWaveTime = 0.75f;
	private Coroutine _shockWaveCoroutine;
	private Material _material;
	private static int _waveDistanceFromCenter = Shader.PropertyToID("_WaveDistanceFromCenter");

	private void Awake()
	{
		_instance = this;
		_material = GetComponent<SpriteRenderer>().material;
	}

	private void Update()
	{
		if(Keyboard.current.f1Key.wasPressedThisFrame)
		{
			CallShockWave();
		}
	}

	public void CallShockWave()
	{
		_shockWaveCoroutine = StartCoroutine(ShockWaveAction(-0.1f, 1f)); 
	}

	private IEnumerator ShockWaveAction(float startPos, float endPos)
	{
		_material.SetFloat(_waveDistanceFromCenter, startPos);

		float lerpedAmount = 0f;
		float elapsedTime = 0f;
		while(elapsedTime < _shockWaveTime)
		{
			elapsedTime += Time.deltaTime;

			lerpedAmount = Mathf.Lerp(startPos, endPos, (elapsedTime /_shockWaveTime));
			_material.SetFloat(_waveDistanceFromCenter, lerpedAmount);

			yield return null;
		}
	}

}
