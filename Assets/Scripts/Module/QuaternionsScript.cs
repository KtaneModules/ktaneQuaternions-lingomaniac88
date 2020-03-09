using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace KtaneQuaternions
{
	public enum Color
	{
		Red,
		Green,
		Blue,
		Yellow,
		White
	}
}

public static class ColorExtensions
{
	public static string ToEquationColor(this KtaneQuaternions.Color color)
	{
		switch (color)
		{
			case KtaneQuaternions.Color.Red:
				return "#ff0000aa";
			case KtaneQuaternions.Color.Green:
				return "#00cc00aa";
			case KtaneQuaternions.Color.Blue:
				return "#3399ffaa";
			case KtaneQuaternions.Color.Yellow:
				return "#ffff00aa";
			case KtaneQuaternions.Color.White:
				return "#ffffffaa";
			default:
				throw new ArgumentException("invalid color value", color.ToString());
		}
	}
}

public class QuaternionsScript : MonoBehaviour
{
	public KMAudio Audio;
	public KMBombInfo BombInfo;
	public KMBombModule Module;

	public Material[] ButtonMaterials;

	public TextMesh EquationText;

	public KMSelectable[] NumberButtons;
	public TextMesh DisplayText;

	public KMSelectable ClearButton;
	public KMSelectable SubmitButton;

	int Answer;

	bool AcceptsInput = false;

	static int ModuleIdCounter = 1;
	int ModuleId;

	void Awake()
	{
		ModuleId = ModuleIdCounter++;
	}

	// Use this for initialization
	void Start()
	{
		Module.OnActivate += delegate()
		{
			AcceptsInput = true;
		};

		// Assign each component with a non-white color
		var colorChannels = new List<KtaneQuaternions.Color> {
			KtaneQuaternions.Color.Red,
			KtaneQuaternions.Color.Green,
			KtaneQuaternions.Color.Blue,
			KtaneQuaternions.Color.Yellow
		}.Shuffle();

		EquationText.text = string.Format("<color={1}>i²</color> = <color={2}>j²</color> = <color={3}>k²</color> = <color={1}>i</color><color={2}>j</color><color={3}>k</color> = <color={0}>−1</color>", colorChannels[0].ToEquationColor(), colorChannels[1].ToEquationColor(), colorChannels[2].ToEquationColor(), colorChannels[3].ToEquationColor());

		ModuleLog("Component colors: real={0}, i={1}, j={2}, k={3}", colorChannels[0], colorChannels[1], colorChannels[2], colorChannels[3]);

		// Assign button colors
		var buttonColors = new List<KtaneQuaternions.Color> {
			KtaneQuaternions.Color.Red,
			KtaneQuaternions.Color.Red,
			KtaneQuaternions.Color.Green,
			KtaneQuaternions.Color.Green,
			KtaneQuaternions.Color.Blue,
			KtaneQuaternions.Color.Blue,
			KtaneQuaternions.Color.Yellow,
			KtaneQuaternions.Color.Yellow,
			KtaneQuaternions.Color.White,
			KtaneQuaternions.Color.White
		}.Shuffle();

		ModuleLog("Button colors: {0}", Enumerable.Range(0, 10).Select(i => string.Format("{0}={1}", i, buttonColors[i])).Join(", "));

		// A cheap way to get a random color without having to generate a new array. Yes, I'm being lazy.
		var submitColor = buttonColors.PickRandom();
		SubmitButton.GetComponentInChildren<Renderer>().material = ButtonMaterials[(int) submitColor];

		int index = colorChannels.IndexOf(submitColor);
		var componentNames = new string[] {"squared norm", "real component", "i component", "j component", "k component"};
		ModuleLog("Submit button is {0}, corresponds to {1}", submitColor, componentNames[index + 1]);

		// Same laziness.
		var clearColor = buttonColors.PickRandom();
		ClearButton.GetComponentInChildren<Renderer>().material = ButtonMaterials[(int) clearColor];

		ModuleLog("Clear button is {0}, but who cares?", clearColor);

		// While we're looping through, keep track of which colors have which numbers
		var componentsByColor = new Dictionary<KtaneQuaternions.Color, HashSet<int>>();

		for (int i = 0; i < 10; i++)
		{
			NumberButtons[i].GetComponentInChildren<Renderer>().material = ButtonMaterials[(int) buttonColors[i]];
			if (!componentsByColor.ContainsKey(buttonColors[i]))
			{
				componentsByColor[buttonColors[i]] = new HashSet<int>();
			}
			componentsByColor[buttonColors[i]].Add(i == 0 ? 10 : i);
		}

		// Treat the components as an array in the meantime.
		// Numbers are stored as [a1, b1, c1, d1, a2, b2, c2, d2].
		var components = new int[] {
			componentsByColor[colorChannels[0]].Max(),
			componentsByColor[colorChannels[1]].Max(),
			componentsByColor[colorChannels[2]].Max(),
			componentsByColor[colorChannels[3]].Max(),
			componentsByColor[colorChannels[0]].Min(),
			componentsByColor[colorChannels[1]].Min(),
			componentsByColor[colorChannels[2]].Min(),
			componentsByColor[colorChannels[3]].Min()
		};

		// Determine inversions, and invert as necessary
		var invertColorRules = new Dictionary<KtaneQuaternions.Color, bool>
		{
			// Red: This color belongs to the i or j component.
			{KtaneQuaternions.Color.Red, colorChannels[1] == KtaneQuaternions.Color.Red || colorChannels[2] == KtaneQuaternions.Color.Red},
			// Green: The bomb has at least one PS/2 port.
			{KtaneQuaternions.Color.Green, BombInfo.IsPortPresent(Port.PS2)},
			// Blue: The bomb's serial number contains a letter in the word BLUE.
			{KtaneQuaternions.Color.Blue, "BLUE".Select(c => BombInfo.GetSerialNumberLetters().Select(char.ToUpperInvariant).Contains(c)).Any(b => b)},
			// Yellow: The sum of the two white keys (this time treating 0 as 0) is prime.
			{KtaneQuaternions.Color.Yellow, new int[] {2, 3, 5, 7, 11, 13, 17}.Contains(componentsByColor[KtaneQuaternions.Color.White].Select(i => i % 10).Sum())}
		};

		if (invertColorRules.Any(kvp => kvp.Value))
		{
			ModuleLog("Applicable Table A rules: {0}", invertColorRules.Where(kvp => kvp.Value).Select(kvp => kvp.Key).Join(", "));
		}
		else
		{
			ModuleLog("Applicable Table A rules: (none)");
		}

		for (int i = 0; i < 4; i++)
		{
			if (invertColorRules[colorChannels[i]])
			{
				int temp = components[i];
				components[i] = components[i + 4];
				components[i + 4] = temp;
			}
		}

		// Serial number inversion check
		var serialDigits = BombInfo.GetSerialNumberNumbers();

		for (int i = 0; i < 8; i++)
		{
			if (serialDigits.Contains(components[i] % 10))
			{
				components[i] *= -1;
			}
		}

		// Quaternion time!
		var q1 = new KtaneQuaternions.Quaternion(components[0], components[1], components[2], components[3]);
		var q2 = new KtaneQuaternions.Quaternion(components[4], components[5], components[6], components[7]);

		if (BombInfo.GetOnIndicators().Count() == 0)
		{
			q1 = q1.Conjugate();
		}

		if (BombInfo.GetOffIndicators().Count() == 0)
		{
			q2 = q2.Conjugate();
		}

		bool oddBatteries = BombInfo.GetBatteryCount() % 2 == 1;
		var q3 = oddBatteries ? (q1 * q2) : (q2 * q1);

		ModuleLog("q₁ = {0}", q1);
		ModuleLog("q₂ = {0}", q2);
		ModuleLog("{0} = {1}", oddBatteries ? "q₁q₂" : "q₂q₁", q3);

		if (submitColor == colorChannels[0])
		{
			Answer = q3.A;
		}
		else if (submitColor == colorChannels[1])
		{
			Answer = q3.B;
		}
		else if (submitColor == colorChannels[2])
		{
			Answer = q3.C;
		}
		else if (submitColor == colorChannels[3])
		{
			Answer = q3.D;
		}
		else
		{
			Answer = q3.SquaredNorm();
		}

		ModuleLog("Correct answer: {0}", Answer);

		// Assign actions
		foreach (var button in NumberButtons.Union(new KMSelectable[] {SubmitButton, ClearButton}))
		{
			button.OnInteract += delegate()
			{
				OnButtonPressed(button);
				return false;
			};
		}
	}
	
	void ModuleLog(string format, params object[] args)
	{
		var prefix = string.Format("[Quaternions #{0}] ", ModuleId);
		Debug.LogFormat(prefix + format, args);
	}

	void OnButtonPressed(KMSelectable button)
	{
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		button.AddInteractionPunch(0.5f);
		button.GetComponentInChildren<Animator>().SetTrigger("PushTrigger");
		if (AcceptsInput)
		{
			if (button == SubmitButton)
			{
				ModuleLog("Submitting {0}...", DisplayText.text);
				if (Answer.ToString() == DisplayText.text)
				{
					ModuleLog("Correct! Module disarmed.");
					AcceptsInput = false;
					Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
					Module.HandlePass();
				}
				else
				{
					ModuleLog("Strike! Incorrect answer.");
					Module.HandleStrike();
				}
			}
			else if (button == ClearButton)
			{
				DisplayText.text = (DisplayText.text.Length == 0) ? "-" : "";
			}
			else if (DisplayText.text.Length < 6)
			{
				DisplayText.text += button.GetComponentInChildren<TextMesh>().text;
			}
		}
		StartCoroutine(PlayReleaseAfterDelay());
	}

	System.Collections.IEnumerator PlayReleaseAfterDelay()
	{
		yield return new WaitForSeconds(0.2f);
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
	}

	#pragma warning disable 414
	private string TwitchHelpMessage = "Use \"!{0} submit 1234\", \"!{0} press 1234\", or \"!{0} 1234\" to submit your answer. Any previous input will automatically be cleared.";
	#pragma warning restore 414

	public KMSelectable[] ProcessTwitchCommand(string command)
	{
		var match = Regex.Match(command.Trim().ToLowerInvariant(), "^(submit +|press +|)(-?\\d+)$");
		if (match.Success)
		{
			var buttonsToPress = new List<KMSelectable>();

			if (DisplayText.text.Length > 0)
			{
				buttonsToPress.Add(ClearButton);
			}

			foreach (char c in match.Groups[2].Value)
			{
				if (c == '-')
				{
					buttonsToPress.Add(ClearButton);
				}
				else if (Char.IsDigit(c))
				{
					buttonsToPress.Add(NumberButtons[c - '0']);
				}
			}

			buttonsToPress.Add(SubmitButton);

			return buttonsToPress.ToArray();
		}
		else
		{
			return null;
		}
	}
}
