using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class GoingBackwardsScript : MonoBehaviour {

	public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
	
	public KMSelectable[] NumberButtons;
	public KMSelectable CenterButton;
	public TextMesh CenterText;
	public TextMesh Timer;
	public AudioClip[] SFX;
	
	string GeneratedNumber, SubmittedNumber = "";
	bool GoingDown, Animating = false;
	Coroutine TheTime;
	
	//Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;
	
	void Awake()
	{
		moduleId = moduleIdCounter++;
		for (int a = 0; a < NumberButtons.Count(); a++)
		{
			int Placement = a;
            NumberButtons[Placement].OnInteract += delegate
            {
                PressNumber(Placement);
				return false;
            };
		}
		CenterButton.OnInteract += delegate () { CenterPress(); return false; };
	}
	
	void CenterPress()
	{
		CenterButton.AddInteractionPunch(.2f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
		if (!ModuleSolved)
		{
			if (CenterText.text == "?")
			{
				StartCoroutine(GeneratedANumber());
			}
			
			if (SubmittedNumber.Length == 10 && !Animating)
			{
				StopCoroutine(TheTime);
				Timer.text = "-";
				string AnswerBeingSent = ReverseString(SubmittedNumber);
				Debug.LogFormat("[Going Backwards #{0}] You submitted: {1}", moduleId, SubmittedNumber);
				if (AnswerBeingSent != GeneratedNumber)
				{
					Debug.LogFormat("[Going Backwards #{0}] The answer was incorrect. Module is resetting.", moduleId);
					StartCoroutine(IncorrectAnswer());
				}
				
				else
				{
					Debug.LogFormat("[Going Backwards #{0}] The answer was correct. Module is being solved.", moduleId);
					StartCoroutine(CorrectAnswer());
				}
			}
		}
	}
	
	string ReverseString(string Texter)
	{
		if (Texter.Length == 0)
		{
			return null;
		}
		char[] charArray = Texter.ToCharArray();
		Array.Reverse(charArray);
		return new string (charArray);
	}
	
	void PressNumber(int Placement)
	{
		NumberButtons[Placement].AddInteractionPunch(.2f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
		if (!ModuleSolved)
		{
			if (GoingDown && !Animating)
			{
				if (SubmittedNumber.Length == 10)
				{
					StopCoroutine(TheTime);
					Timer.text = "-";
					string AnswerBeingSent = ReverseString(SubmittedNumber);
					Debug.LogFormat("[Going Backwards #{0}] You submitted: {1}", moduleId, SubmittedNumber);
					if (AnswerBeingSent != GeneratedNumber)
					{
						Debug.LogFormat("[Going Backwards #{0}] The answer was incorrect. Module is resetting.", moduleId);
						StartCoroutine(IncorrectAnswer());
					}
					
					else
					{
						Debug.LogFormat("[Going Backwards #{0}] The answer was correct. Module is being solved.", moduleId);
						StartCoroutine(CorrectAnswer());
					}
				}
				
				else
				{
					SubmittedNumber += Placement.ToString();
				}
			}
		}
	}
	
	IEnumerator GeneratedANumber()
	{
		Animating = true;
		CenterText.text = "";
		yield return new WaitForSecondsRealtime(0.3f);
		for (int x = 0; x < 10; x++)
		{
			int Coral = UnityEngine.Random.Range(0,10);
			CenterText.text = Coral.ToString();
			GeneratedNumber += Coral.ToString();
			yield return new WaitForSecondsRealtime(0.3f);
			CenterText.text = "";
			if (x != 9) {
			yield return new WaitForSecondsRealtime(0.3f);
			}
		}
		TheTime = StartCoroutine(TimeIsRunningOut());
		GoingDown = true;
		Animating = false;
		Debug.LogFormat("[Going Backwards #{0}] The number generated: {1}", moduleId, GeneratedNumber);
		Debug.LogFormat("[Going Backwards #{0}] The correct answer: {1}", moduleId, ReverseString(GeneratedNumber));
	}
	
	IEnumerator TimeIsRunningOut()
	{
		Timer.text = "32";
		while (Timer.text != "0")
		{
			yield return new WaitForSecondsRealtime(1f);
			Timer.text = (Int32.Parse(Timer.text) - 1).ToString();
		}
		Timer.color = Color.red;
		Debug.LogFormat("[Going Backwards #{0}] You ran out of time. Module is resetting.", moduleId);
		StartCoroutine(IncorrectAnswer());
	}
	
	IEnumerator CorrectAnswer()
	{
		Animating = true;
		string Apple = "YOU SOLVED IT";
		CenterText.color = Color.green;
		for (int x = 0; x < Apple.Length; x++)
		{
			CenterText.text = Apple[x].ToString();
			yield return new WaitForSecondsRealtime(0.1f);
		}
		Module.HandlePass();
		CenterText.color = Color.white;
		CenterText.text = "!";
		Timer.text = "GG";
		ModuleSolved = true;
		Audio.PlaySoundAtTransform(SFX[0].name, transform);
	}
	
	IEnumerator IncorrectAnswer()
	{
		Animating = true;
		string Apple = "NO";
		CenterText.color = Color.red;
		for (int x = 0; x < 20; x++)
		{
			CenterText.text = Apple[x%2].ToString();
			yield return new WaitForSecondsRealtime(0.1f);
		}
		Module.HandleStrike();
		CenterText.color = Color.white;
		Timer.color = Color.white;
		CenterText.text = "?";
		Timer.text = "??";
		Animating = false;
		GoingDown = false;
		GeneratedNumber = "";
		SubmittedNumber = "";
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"To start the sequence in the module, use !{0} play/playfocus | To submit the answer on the module, use !{0} submit [10 digit number]";
    #pragma warning restore 414
	
	string[] ValidNumbers = {"1", "2", "3", "4", "5", "6", "7", "8", "9", "0"};
	
	IEnumerator ProcessTwitchCommand(string command)
	{
		string[] parameters = command.Split(' ');
		if (Animating)
		{
			yield return "sendtochaterror The module is performing an animation. Command ignored.";
			yield break;
		}
			
		if (Regex.IsMatch(command, @"^\s*play\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (GoingDown)
			{
				yield return "sendtochaterror The module is active. Command ignored due to potential of breaking the answer.";
				yield break;
			}
			CenterButton.OnInteract();
		}
		
		if (Regex.IsMatch(command, @"^\s*playfocus\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (GoingDown)
			{
				yield return "sendtochaterror The module is active. Command ignored due to potential of breaking the answer.";
				yield break;
			}
			
			CenterButton.OnInteract();
			while (Animating == true)
			{
				yield return null;
			}
		}
		
		if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (parameters.Length != 2)
			{
				yield return "sendtochaterror Invalid parameter length. Command ignored.";
				yield break;
			}
			
			if (parameters[1].Length != 10)
			{
				yield return "sendtochaterror Number length is not 10. Command ignored.";
				yield break;
			}
			
			for (int x = 0; x < 10; x++)
			{
				if (!parameters[1][x].ToString().EqualsAny(ValidNumbers))
				{
					yield return "sendtochaterror The number being sent contains an invalid character. Command ignored.";
					yield break;
				}
			}
			
			for (int x = 0; x < 10; x++)
			{
				NumberButtons[Int32.Parse(parameters[1][x].ToString())].OnInteract();
				yield return new WaitForSecondsRealtime(0.1f);
			}
			
			yield return "strike";
			yield return "solve";
			CenterButton.OnInteract();
		}
	}
}