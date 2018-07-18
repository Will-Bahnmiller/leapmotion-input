using UnityEngine;
using System.Collections;
using System;

public class InputManager : MonoBehaviour
{
	/* ==================== Input Manager Member Variables ======================= */
	// In-Editor
	[SerializeField]
	private float dwellTime;
	public bool debugMode;

	public static InputManager _instance = null;
	public delegate void ASLAlphabetDetectedHandler(char alphabet);
	public delegate void ASLSimilarityDetectedHandler(ASLTest similarity);

	[HideInInspector]
	public static readonly char INVALID_ASL_LETTER = ' ';

	private Leap.Controller leapController;
	private MyLeapListener leapListener;
	private ASLAlphabetDetectedHandler letterHandler;
	private ASLSimilarityDetectedHandler similarityHandler;


	private long lastLetterTime;
	private char currentLetter;
	private char lastResult;
	private ASLTest currentSimilarity;

	/* ================================= Accessors =================================== */

	/* Get and Set the Dwell Time, or the time the letter position needs to stay in before it's considered valid.*/
	public static float DwellTime
	{
		get
		{
			return _instance.dwellTime;
		}
		set
		{
			_instance.dwellTime = value;
		}
	}

	/* Polling Method to get current letter detected */
	public static char GetLetter()
	{
		return (_instance != null) ? char.ToLower(_instance.currentLetter) : INVALID_ASL_LETTER;
	}
	public static bool CompareLetter(char letter)
	{
		return (_instance != null) && char.ToLower(_instance.currentLetter) == char.ToLower(letter);
	}


	/* ================================ Update Calls ================================== */

	/* Set Current Letter */
	private void UpdateCurrentLetter(char letter)
	{
		if (currentLetter != letter)
		{
			currentLetter = letter;
			if (letterHandler != null)
			{
				if (debugMode) Debug.Log("ASL Letter Handler Called. Value: " + letter);
				letterHandler(letter);
			}
		}
	}

	/* Set Current Similarity Result */
	private void UpdateCurrentSimilarity(ASLTest test)
	{
		if (currentSimilarity != test)
		{
			currentSimilarity = test;
			if (similarityHandler != null)
			{
				if (debugMode) Debug.Log("ASL Similary Handler Called. Value: " + test);
				similarityHandler(test);
			}
		}
	}

	/* ========================= Initialize Data Members ============================ */
	// Singleton Approach
	public static void Initialize(InputManager instance, ASLAlphabetDetectedHandler letterCallback = null, ASLSimilarityDetectedHandler similarityCallback = null)
	{
		// Initialize
		instance.Init(letterCallback, similarityCallback);
	}
	public static void Initialize(InputManager instance, ASLSimilarityDetectedHandler similarityHandler)
	{
		Initialize(instance, null, similarityHandler);
	}

	/* Initialize All Members*/
	public void Init(ASLAlphabetDetectedHandler letterCallback = null, ASLSimilarityDetectedHandler similarityCallback = null)
	{
		MyLeapListener listener;
		Leap.Controller controller;
		if (_instance != null)
		{
			listener = _instance.leapListener;
			controller = _instance.leapController;
		}
		else
		{
			listener = new MyLeapListener();
			controller = new Leap.Controller(listener);
		}
		_instance = this;

		// Set-up the Listener
		leapListener = listener;
		leapController = controller;

		// Initialize Local Data
		letterHandler = letterCallback;
		similarityHandler = similarityCallback;

		lastLetterTime = DateTime.Now.Ticks;
		currentLetter = INVALID_ASL_LETTER;
		currentSimilarity = ASLTest.INVALID_ASL_TEST;
		lastResult = INVALID_ASL_LETTER;

		FirstPassInit();
		SecondPassInit();
	}

	public void Init(ASLSimilarityDetectedHandler similarityCallback)
	{
		Init(null, similarityCallback);
	}

	/* ========================= Create Event Triggers ============================ */
	private class MyLeapListener : Leap.Listener
	{
		/*	Order of Calls:			
		 *		OnInit				(Initialized)
		 *		OnServiceConnect	(Service is Connected)
		 *		OnConnect			(Leap Motion Connected)
		 *		OnFrame				(New Frame Available)
		 *	Note:
		 *		Once FocusLost triggers, OnFrame will not call until FocusGain triggers
		 *		Once OnDisconnect triggers, OnFrame will not call until OnConnect triggers
		 */

		private static bool updateDebugLogOnFrame = true;

		/* The first method called in this Listener */
		public override void OnInit(Leap.Controller controller)
		{
			Debug.Log("Initialized");
			updateDebugLogOnFrame = true;
		}

		/* This is just in case, since there should always be a service on */
		public override void OnServiceConnect(Leap.Controller controller)
		{
			Debug.Log("Service is connected.");
			updateDebugLogOnFrame = true;
		}
		public override void OnServiceDisconnect(Leap.Controller controller)
		{
			Debug.Log("Service is disconnected.");
			updateDebugLogOnFrame = true;
		}

		/* Check whether a Leap Motion is connected*/
		public override void OnConnect(Leap.Controller controller)
		{
			Debug.Log("Leap Motion Connected");
			updateDebugLogOnFrame = true;
		}
		public override void OnDisconnect(Leap.Controller controller)
		{
			Debug.Log("Leap Motion Disconnected");
			updateDebugLogOnFrame = true;
		}

		/*	Check whether this current application has the Leap Motion focus */
		public override void OnFocusGained(Leap.Controller controller)
		{
			Debug.Log("Focus gained");
			updateDebugLogOnFrame = true;
		}
		public override void OnFocusLost(Leap.Controller controller)
		{
			Debug.Log("Focus lost");
			updateDebugLogOnFrame = true;
		}

		/* React to the New Frame Received from the Leap Motion */
		public override void OnFrame(Leap.Controller controller)
		{
			// Get Hand from this current Frame
			InputManager IM = _instance;
			Leap.HandList hands = controller.Frame().Hands;
			// Restrict to One Hand Detection
			if (hands.Count == 1)
			{
				// Call Leap Motion Update
				try
				{
					OnLeapMotionUpdate(hands.Frontmost);
					Debug.Log("Hand Detected");
				}
				catch (Exception e)
				{
					Debug.Log(e.ToString());
					Debug.Log(e.ToString());
				}
			}
			else if (IM.currentSimilarity != ASLTest.INVALID_ASL_TEST)
			{
				IM.UpdateCurrentSimilarity(ASLTest.INVALID_ASL_TEST);
				IM.UpdateCurrentLetter(INVALID_ASL_LETTER);
				/*
				//Handle case of Leap Motion with no more detection
				IM.currentLetter = INVALID_ASL_LETTER;
				IM.similarityHandler(ASLTest.INVALID_ASL_TEST);
				IM.letterHandler(INVALID_ASL_LETTER);
				IM.currentLetter = INVALID_ASL_LETTER;*/
			}

			// For Debugging Purposes only
			if (updateDebugLogOnFrame)
			{
				Debug.Log("New Frame available");
				updateDebugLogOnFrame = false;
			}
		}

		/* This is just in case the controller removes this listener or is destroyed */
		public override void OnExit(Leap.Controller controller)
		{
			Debug.Log("Exited");
			updateDebugLogOnFrame = true;
		}
	}


	/* ========================= Leap Motion Utility Functions ============================ */

	private Vector3 ToVector3(Leap.Vector vector)
	{
		return new Vector3(vector.x, vector.y, vector.z);
	}

	/* ========================= Initialize Tracking Algorithms ============================ */

	/* Method for the Distance Between Palm and Pointer */
	private static void OnLeapMotionUpdate(Leap.Hand hand)
	{
		// Get Instance
		InputManager IM = _instance;
		if (IM == null) return;

		// Complete First Pass
		FingerLift[] fingerTest = IM.RetrieveFingerLift(hand);
		ASLTest firstPassResults = IM.FirstPassAlgorithm(fingerTest);

		// Call Similarity Handler
		IM.UpdateCurrentSimilarity(firstPassResults);
		/*if(IM.similarityHandler != null)
		{
			IM.similarityHandler(firstPassResults);
		}*/

		if (firstPassResults == ASLTest.INVALID_ASL_TEST)
		{
			IM.UpdateCurrentLetter(INVALID_ASL_LETTER);
			return;
		}

		// Complete Second Pass
		char secondPassResults = IM.SecondPassAlgorithm(firstPassResults, hand);

		// Determine if Successful
		bool finalResults = (secondPassResults >= 'A' && secondPassResults <= 'Z') || (secondPassResults >= 'a' && secondPassResults <= 'z');

		if (IM.lastResult != secondPassResults)
		{
			IM.lastResult = secondPassResults;
			IM.lastLetterTime = DateTime.Now.Ticks;
		}

		// Call Letter Handler
		if (finalResults)
		{
			if (((DateTime.Now.Ticks - IM.lastLetterTime) / TimeSpan.TicksPerMillisecond) >= (IM.dwellTime * 1000))
			{
				IM.UpdateCurrentLetter(secondPassResults);
			}
			else
			{
				IM.UpdateCurrentLetter(INVALID_ASL_LETTER);
			}
		}
		else
		{
			IM.UpdateCurrentLetter(INVALID_ASL_LETTER);
		}
	}

	/* ========================== First-Pass Algorithms Member Values ======================= */
	private enum FingerLift
	{
		INVALID_FINGER_LIFT = -1,
		FINGER_UP,
		FINGER_MIDDLE,
		FINGER_DOWN,
		NUM_OF_FINGER_LIFT,
		MAX_FINGER_LIFT = NUM_OF_FINGER_LIFT
	}

	// TODO: Find better way to shorten long syntax
	private enum MyFingerType
	{
		TYPE_THUMB = Leap.Finger.FingerType.TYPE_THUMB,
		TYPE_INDEX = Leap.Finger.FingerType.TYPE_INDEX,
		TYPE_MIDDLE = Leap.Finger.FingerType.TYPE_MIDDLE,
		TYPE_RING = Leap.Finger.FingerType.TYPE_RING,
		TYPE_PINKY = Leap.Finger.FingerType.TYPE_PINKY
	}
	public enum ASLTest
	{
		INVALID_ASL_TEST = -1,
		TEST_AEMNST,
		TEST_B,
		TEST_CO,
		TEST_DGPQZ,
		TEST_F,
		TEST_HKRUV,
		TEST_IJ,
		TEST_L,
		TEST_W,
		TEST_X,
		TEST_Y,
		NUM_OF_ASL_TESTS,
		MAX_ASL_TESTS = NUM_OF_ASL_TESTS
	}


	/* ======================== First-Pass Algorithms Member Variables ====================== */

	private ASLTest[][][][][] ASLTestMapping;

	private int[] DownDistanceThreshold;
	private int[] MiddleDistanceThreshold;


	/* ========================= Initialize First-Pass Algorithms ========================== */
	void FirstPassInit()
	{
		// Distance Threshold Initialize
		DownDistanceThreshold = new int[5];
		DownDistanceThreshold[(int)MyFingerType.TYPE_THUMB] = 10;
		DownDistanceThreshold[(int)MyFingerType.TYPE_INDEX] = 65;   //70;
		DownDistanceThreshold[(int)MyFingerType.TYPE_MIDDLE] = 65;  //70;
		DownDistanceThreshold[(int)MyFingerType.TYPE_RING] = 65;    //70;
		DownDistanceThreshold[(int)MyFingerType.TYPE_PINKY] = 60;   //60;

		MiddleDistanceThreshold = new int[5];
		MiddleDistanceThreshold[(int)MyFingerType.TYPE_THUMB] = 50;
		MiddleDistanceThreshold[(int)MyFingerType.TYPE_INDEX] = 80;
		MiddleDistanceThreshold[(int)MyFingerType.TYPE_MIDDLE] = 80;
		MiddleDistanceThreshold[(int)MyFingerType.TYPE_RING] = 80;
		MiddleDistanceThreshold[(int)MyFingerType.TYPE_PINKY] = 70;

		// ASL Test Map Initialize
		ASLTestMapping = new ASLTest[3][][][][];
		for (int i = 0; i < 3; ++i)
		{
			ASLTestMapping[i] = new ASLTest[3][][][];
			for (int j = 0; j < 3; ++j)
			{
				ASLTestMapping[i][j] = new ASLTest[3][][];
				for (int k = 0; k < 3; ++k)
				{
					ASLTestMapping[i][j][k] = new ASLTest[3][];
					for (int l = 0; l < 3; ++l)
					{
						ASLTestMapping[i][j][k][l] = new ASLTest[3];
						for (int m = 0; m < 3; ++m)
						{
							ASLTestMapping[i][j][k][l][m] = ASLTest.INVALID_ASL_TEST;
						}
					}
				}
			}
		}

		int DOWN = (int)FingerLift.FINGER_DOWN;
		int MID_ = (int)FingerLift.FINGER_MIDDLE;
		int UP__ = (int)FingerLift.FINGER_UP;

		// Initialize the ASLTest Function calls to its respective ASL Test enumeration

		//=============== TEST A-E-M-N-S-T Test ==========================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		ASLTestMapping[DOWN][DOWN][DOWN][DOWN][DOWN] = ASLTest.TEST_AEMNST;
		ASLTestMapping[MID_][DOWN][DOWN][DOWN][DOWN] = ASLTest.TEST_AEMNST;

		//=============== TEST H-K-R-U-V Test ============================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		ASLTestMapping[DOWN][UP__][UP__][DOWN][DOWN] = ASLTest.TEST_HKRUV;
		ASLTestMapping[MID_][UP__][UP__][DOWN][DOWN] = ASLTest.TEST_HKRUV;

		//================ TEST D-G-P-Q-Z Test ===========================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		ASLTestMapping[DOWN][UP__][DOWN][DOWN][DOWN] = ASLTest.TEST_DGPQZ;
		ASLTestMapping[MID_][UP__][DOWN][DOWN][DOWN] = ASLTest.TEST_DGPQZ;

		ASLTestMapping[MID_][UP__][DOWN][DOWN][DOWN] = ASLTest.TEST_DGPQZ;
		ASLTestMapping[MID_][UP__][DOWN][DOWN][MID_] = ASLTest.TEST_DGPQZ;
		ASLTestMapping[MID_][UP__][DOWN][MID_][DOWN] = ASLTest.TEST_DGPQZ;
		ASLTestMapping[MID_][UP__][DOWN][MID_][MID_] = ASLTest.TEST_DGPQZ;
		ASLTestMapping[MID_][UP__][MID_][DOWN][DOWN] = ASLTest.TEST_DGPQZ;
		ASLTestMapping[MID_][UP__][MID_][DOWN][MID_] = ASLTest.TEST_DGPQZ;
		ASLTestMapping[MID_][UP__][MID_][MID_][DOWN] = ASLTest.TEST_DGPQZ;
		ASLTestMapping[MID_][UP__][MID_][MID_][MID_] = ASLTest.TEST_DGPQZ;
		ASLTestMapping[DOWN][UP__][MID_][MID_][MID_] = ASLTest.TEST_DGPQZ;

		//====================== TEST L Test =============================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		ASLTestMapping[UP__][UP__][DOWN][DOWN][DOWN] = ASLTest.TEST_L;

		//==================== TEST I-J Test =============================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		ASLTestMapping[DOWN][DOWN][DOWN][DOWN][UP__] = ASLTest.TEST_IJ;
		ASLTestMapping[MID_][DOWN][DOWN][DOWN][UP__] = ASLTest.TEST_IJ;

		//==================== TEST C-O Test =============================
		//_____________THMB__INDX__MID___RING__PNKY_______________________

		ASLTestMapping[MID_][UP__][UP__][UP__][UP__] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][UP__][UP__][UP__][MID_] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][UP__][UP__][MID_][UP__] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][UP__][UP__][MID_][MID_] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][UP__][MID_][UP__][UP__] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][UP__][MID_][UP__][MID_] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][UP__][MID_][MID_][UP__] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][UP__][MID_][MID_][MID_] = ASLTest.TEST_CO;       // Same as PQ
		//ASLTestMapping[MID_][MID_][UP__][UP__][UP__] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][MID_][UP__][UP__][MID_] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][MID_][UP__][MID_][UP__] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][MID_][UP__][MID_][MID_] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][MID_][MID_][UP__][UP__] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][MID_][MID_][UP__][MID_] = ASLTest.TEST_CO;
		//ASLTestMapping[MID_][MID_][MID_][MID_][UP__] = ASLTest.TEST_CO;
		ASLTestMapping[MID_][MID_][MID_][MID_][MID_] = ASLTest.TEST_CO;
		ASLTestMapping[DOWN][MID_][MID_][MID_][MID_] = ASLTest.TEST_CO;

		//===================== TEST B Test ==============================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		ASLTestMapping[DOWN][UP__][UP__][UP__][UP__] = ASLTest.TEST_B;

		//====================== TEST F Test =============================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		//ASLTestMapping[DOWN][DOWN][UP__][UP__][UP__] = ASLTest.TEST_F;
		ASLTestMapping[MID_][DOWN][UP__][UP__][UP__] = ASLTest.TEST_F;
		ASLTestMapping[MID_][MID_][UP__][UP__][UP__] = ASLTest.TEST_F;

		//==================== TEST W Test ===============================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		ASLTestMapping[DOWN][UP__][UP__][UP__][DOWN] = ASLTest.TEST_W;
		ASLTestMapping[DOWN][UP__][UP__][UP__][MID_] = ASLTest.TEST_W;
		/*ASLTestMapping[DOWN][MID_][UP__][UP__][DOWN] = ASLTest.TEST_W;
		ASLTestMapping[DOWN][UP__][UP__][MID_][DOWN] = ASLTest.TEST_W;
		ASLTestMapping[DOWN][MID_][UP__][MID_][DOWN] = ASLTest.TEST_W;
		ASLTestMapping[DOWN][UP__][MID_][UP__][DOWN] = ASLTest.TEST_W;
		ASLTestMapping[DOWN][MID_][MID_][UP__][DOWN] = ASLTest.TEST_W;
		ASLTestMapping[DOWN][UP__][MID_][MID_][DOWN] = ASLTest.TEST_W;
		ASLTestMapping[DOWN][MID_][MID_][MID_][DOWN] = ASLTest.TEST_W;*/

		//===================== TEST X Test ==============================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		ASLTestMapping[DOWN][MID_][DOWN][DOWN][DOWN] = ASLTest.TEST_X;
		ASLTestMapping[MID_][MID_][DOWN][DOWN][DOWN] = ASLTest.TEST_X;
		// Middle or up

		//==================== TEST Y Test ===============================
		//_____________THMB__INDX__MID___RING__PNKY_______________________
		ASLTestMapping[UP__][DOWN][DOWN][DOWN][UP__] = ASLTest.TEST_Y;
		ASLTestMapping[UP__][DOWN][DOWN][DOWN][MID_] = ASLTest.TEST_Y;
	}

	private FingerLift[] RetrieveFingerLift(Leap.Hand hand)
	{
		// Initialize Finger List
		FingerLift[] fingerTest = new FingerLift[5];
		fingerTest[0] = FingerLift.INVALID_FINGER_LIFT;
		fingerTest[1] = FingerLift.INVALID_FINGER_LIFT;
		fingerTest[2] = FingerLift.INVALID_FINGER_LIFT;
		fingerTest[3] = FingerLift.INVALID_FINGER_LIFT;
		fingerTest[4] = FingerLift.INVALID_FINGER_LIFT;

		Leap.Bone fingerBone;
		string message = "[";
		float distance;
		foreach (Leap.Finger finger in hand.Fingers)
		{
			fingerBone = finger.Bone(Leap.Bone.BoneType.TYPE_DISTAL);

			//Debug.Log("FINGET BONEQW!!   " + fingerBone.Center);
			//Vector3 w = new Vector3(fingerBone.Center.x, fingerBone.Center.y, fingerBone.Center.z);

			if (finger.Type == Leap.Finger.FingerType.TYPE_THUMB)
			{
				Vector3 N = Vector3.Cross(ToVector3(hand.Direction), ToVector3(hand.PalmNormal));
				Vector3 w = ToVector3(finger.TipPosition - hand.PalmPosition);

				Vector3 projection = Vector3.Project(w, N);

				distance = projection.magnitude;
				distance *= (projection.normalized == N.normalized) ? 1.0f : -1.0f;
				distance *= (hand.IsRight) ? 1.0f : -1.0f;
			}
			else
			{
				Vector3 palmPosition = new Vector3(hand.PalmPosition.x, hand.PalmPosition.y, hand.PalmPosition.z);
				Vector3 bonePosition = new Vector3(fingerBone.Center.x, fingerBone.Center.y, fingerBone.Center.z);
				distance = Vector3.Distance(palmPosition, bonePosition);
			}

			// Do Distance Check
			if (distance < DownDistanceThreshold[(int)finger.Type])
			{
				// Finger is Down
				fingerTest[(int)finger.Type] = FingerLift.FINGER_DOWN;
			}
			else if (distance < MiddleDistanceThreshold[(int)finger.Type])
			{
				// Finger is Middle
				fingerTest[(int)finger.Type] = FingerLift.FINGER_MIDDLE;
			}
			else
			{
				// Finger is Up
				fingerTest[(int)finger.Type] = FingerLift.FINGER_UP;
			}

			message += finger.Type + "=" + distance + " (" + fingerTest[(int)finger.Type] + ")      ";
		}
		//Debug.Log(message + ']');

		return fingerTest;
	}

	/* Return the ASL Test that should be tested to guarantee that it is the valid letter */
	private ASLTest FirstPassAlgorithm(FingerLift[] fingerTest)
	{
		// (For Reassurance Purposes)
		if (fingerTest == null)
		{
			Debug.Log("Finger Result is Null. This should not happen.");
			return ASLTest.INVALID_ASL_TEST;
		}

		int thumb = (int)fingerTest[(int)MyFingerType.TYPE_THUMB];
		int index = (int)fingerTest[(int)MyFingerType.TYPE_INDEX];
		int middle = (int)fingerTest[(int)MyFingerType.TYPE_MIDDLE];
		int ring = (int)fingerTest[(int)MyFingerType.TYPE_RING];
		int pinky = (int)fingerTest[(int)MyFingerType.TYPE_PINKY];

		if (thumb < 0 || index < 0 || middle < 0 || ring < 0 || pinky < 0 ||
			thumb > 2 || index > 2 || middle > 2 || ring > 2 || pinky > 2)
		{
			Debug.Log("This Should Not Happen....");
			return ASLTest.INVALID_ASL_TEST;
		}

		return ASLTestMapping[thumb][index][middle][ring][pinky];
		/*
		// Note: This method can be converted to a Finite-State Machine / Graph / Tree for code cleanliness
		// Complete First Pass Algorithm
		if(fingerTest[(int)MyFingerType.TYPE_MIDDLE] == FingerLift.FINGER_DOWN)
		{
			if (fingerTest[(int)MyFingerType.TYPE_INDEX] == FingerLift.FINGER_DOWN)
			{
				if (fingerTest[(int)MyFingerType.TYPE_PINKY] == FingerLift.FINGER_DOWN)
				{
					// ================== Letter A-E-M-N-S-T Test ==================
					if(fingerTest[(int)MyFingerType.TYPE_THUMB] != FingerLift.FINGER_UP
						&& fingerTest[(int)MyFingerType.TYPE_PINKY] == FingerLift.FINGER_DOWN)
					{
						return ASLTest.TEST_AEMNST;
					} else
					{
						return ASLTest.INVALID_ASL_TEST;
					}
				}
				else // if Pinky is Up (or Middle)
				{
                    if (fingerTest[(int)MyFingerType.TYPE_THUMB] == FingerLift.FINGER_UP) 
					{
						// ===================== Letter Y Test =====================
						if (fingerTest[(int)MyFingerType.TYPE_RING] == FingerLift.FINGER_DOWN)
						{
							return ASLTest.TEST_Y;
						}
						else
						{
							return ASLTest.INVALID_ASL_TEST;
						}
					}
					else // if Thumb is Down or Middle
					{
					     // ===================== Letter IJ Test =====================
						if (fingerTest[(int)MyFingerType.TYPE_RING] == FingerLift.FINGER_DOWN)
						{
							return ASLTest.TEST_IJ;
						}
						else
						{
							return ASLTest.INVALID_ASL_TEST;
						}
					}
				}
			}
			else if(fingerTest[(int)MyFingerType.TYPE_INDEX] == FingerLift.FINGER_MIDDLE)
            {
				// ========================== Letter X Test ==========================
				if (fingerTest[(int)MyFingerType.TYPE_THUMB] == FingerLift.FINGER_DOWN
					&& fingerTest[(int)MyFingerType.TYPE_PINKY] == FingerLift.FINGER_DOWN
					&& fingerTest[(int)MyFingerType.TYPE_RING] == FingerLift.FINGER_DOWN)
				{
					return ASLTest.TEST_X;
				}
				else
				{
					return ASLTest.INVALID_ASL_TEST;
				}
			}
			else // if Index is Up
			{
				if (fingerTest[(int)MyFingerType.TYPE_THUMB] == FingerLift.FINGER_DOWN)
				{
					// ========================= Letter D-G-Z =========================
					if (fingerTest[(int)MyFingerType.TYPE_RING] == FingerLift.FINGER_DOWN
						&& fingerTest[(int)MyFingerType.TYPE_PINKY] == FingerLift.FINGER_DOWN)
					{
						return ASLTest.TEST_DGZ;
					}
					else
					{
						return ASLTest.INVALID_ASL_TEST;
					}
				}
				else // if Thumb is Up (or Middle)
				{
					// ======================= Letter LPQ Test =======================
					if (fingerTest[(int)MyFingerType.TYPE_RING] == FingerLift.FINGER_DOWN
						&& fingerTest[(int)MyFingerType.TYPE_PINKY] == FingerLift.FINGER_DOWN)
					{
						return ASLTest.TEST_LPQ;
					}
					else
					{
						return ASLTest.INVALID_ASL_TEST;
					}
				}
			}
		}
		else // If Middle is Up (or Middle)
		{
			if (fingerTest[(int)MyFingerType.TYPE_RING] == FingerLift.FINGER_DOWN)
			{
				// ====================== Letter H-K-R-U-V Test ======================
				if (fingerTest[(int)MyFingerType.TYPE_THUMB] == FingerLift.FINGER_DOWN
				 && fingerTest[(int)MyFingerType.TYPE_PINKY] == FingerLift.FINGER_DOWN
				 && fingerTest[(int)MyFingerType.TYPE_INDEX] == FingerLift.FINGER_UP)
				{
					return ASLTest.TEST_HKRUV;
				}
				else
				{
					return ASLTest.INVALID_ASL_TEST;
				}
			}
			else // If Ring is Up (or Middle)
			{
				if (fingerTest[(int)MyFingerType.TYPE_THUMB] == FingerLift.FINGER_DOWN)
				{
					if (fingerTest[(int)MyFingerType.TYPE_INDEX] == FingerLift.FINGER_DOWN)
					{
						// ====================== Letter F Test ======================
						if (fingerTest[(int)MyFingerType.TYPE_PINKY] == FingerLift.FINGER_UP)
						{
							return ASLTest.TEST_F;
						}
						else
						{
							return ASLTest.INVALID_ASL_TEST;
						}
					}
					else // if Index is Up (or Middle)
					{
						if (fingerTest[(int)MyFingerType.TYPE_PINKY] == FingerLift.FINGER_DOWN)
						{
							// ==================== Letter W Test ====================
							return ASLTest.TEST_W;
						}
						else // if Pinky is Up (or Middle)
						{
							// ==================== Letter B Test ====================
							return ASLTest.TEST_B;
						}
					}
				}
				else // if Thumb is Middle (or Up)
				{
					// ================== Letter C-O Test ==================
					if (fingerTest[(int)MyFingerType.TYPE_INDEX] == FingerLift.FINGER_UP
						&& fingerTest[(int)MyFingerType.TYPE_PINKY] == FingerLift.FINGER_UP)
					{
						return ASLTest.TEST_CO;
					}
					else
					{
						return ASLTest.INVALID_ASL_TEST;
					}
				}
			}
		}
		*/
	}


	/* ======================== Second-Pass Algorithms Member Variables ====================== */

	private delegate char ASLTestFunction(Leap.Hand hand);

	private ASLTestFunction[] DoASLTestCall = new ASLTestFunction[(int)ASLTest.NUM_OF_ASL_TESTS];


	/* ======================== Initialize Second-Pass Algorithm ====================== */
	void SecondPassInit()
	{
		// Initialize the ASLTest Function calls to its respective ASL Test enumeration
		DoASLTestCall[(int)ASLTest.TEST_AEMNST] = DoASLTest_AEMNST;
		DoASLTestCall[(int)ASLTest.TEST_B] = DoASLTest_B;
		DoASLTestCall[(int)ASLTest.TEST_CO] = DoASLTest_CO;
		DoASLTestCall[(int)ASLTest.TEST_DGPQZ] = DoASLTest_DGPQZ;
		DoASLTestCall[(int)ASLTest.TEST_F] = DoASLTest_F;
		DoASLTestCall[(int)ASLTest.TEST_HKRUV] = DoASLTest_HKRUV;
		DoASLTestCall[(int)ASLTest.TEST_IJ] = DoASLTest_IJ;
		DoASLTestCall[(int)ASLTest.TEST_L] = DoASLTest_L;
		DoASLTestCall[(int)ASLTest.TEST_W] = DoASLTest_W;
		DoASLTestCall[(int)ASLTest.TEST_X] = DoASLTest_X;
		DoASLTestCall[(int)ASLTest.TEST_Y] = DoASLTest_Y;

		// For the Programmer's Protection / Piece of Mind.
		for (int i = (int)ASLTest.NUM_OF_ASL_TESTS - 1; i >= 0; i--)
		{
			if (DoASLTestCall[i] == null)
			{
				Debug.Log("Function for " + ((ASLTest)i) + " Not Initialized!");
				Debug.LogError("Function for " + ((ASLTest)i) + " Not Initialized!");
			}
		}
	}

	/* Returns the alphabet that this algorithm has detected, a non-alphabet (whitespace) otherwise */
	private char SecondPassAlgorithm(ASLTest test, Leap.Hand hand)
	{
		// (For Reassurance Purposes)
		if (hand == null)
		{
			Debug.Log("Lift Result is Null. This should not happen.");
			return INVALID_ASL_LETTER;
		}

		// Call Respective ASL Test (using fancy delegates!)
		return DoASLTestCall[(int)test](hand);
	}

	private bool AreTwoFingersInContact(MyFingerType firstFingerType, MyFingerType secondFingerType, Leap.Hand hand, float threshold)
	{
		Leap.FingerList fingers = hand.Fingers;
		Leap.Finger firstFinger = fingers[(int)firstFingerType];
		Leap.Finger secondFinger = fingers[(int)secondFingerType];

		Vector3 firstFingerPos = ToVector3(firstFinger.TipPosition);
		Vector3 secondFingerPos = ToVector3(secondFinger.TipPosition);

		float twoFingerDist = Vector3.Distance(firstFingerPos, secondFingerPos);

		return (twoFingerDist <= threshold);
	}

	private char DoASLTest_AEMNST(Leap.Hand hand)
	{
		// TODO: Add The Proper Letter Detection Here
		// Would be doing this if Leap Motion proved accurate enough...

		//float fingerThreshold = 30;
		//float angleAlignmentThreshold = 15;
		//float pinkyThreshold = 15;
		//float closestFingerThreshold = 10;


		Leap.FingerList fingers = hand.Fingers;
		//Leap.Finger pinky = fingers[(int)Leap.Finger.FingerType.TYPE_PINKY];
		//Leap.Finger ring = fingers[(int)Leap.Finger.FingerType.TYPE_RING];
		//Leap.Finger middle = fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE];
		Leap.Finger index = fingers[(int)Leap.Finger.FingerType.TYPE_INDEX];
		Leap.Finger thumb = fingers[(int)Leap.Finger.FingerType.TYPE_THUMB];

		{ // Test For 'A'
			float distance;
			Vector3 point1 = ToVector3(index.TipPosition);
			Vector3 point2 = ToVector3(index.Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).PrevJoint);
			Vector3 point3 = ToVector3(index.Bone(Leap.Bone.BoneType.TYPE_METACARPAL).PrevJoint);
			Vector3 vector1 = point1 - point2;
			Vector3 vector2 = point3 - point2;
			Vector3 N = Vector3.Cross(vector1, vector2);
			Vector3 w = ToVector3(thumb.TipPosition) - point2;

			Vector3 projection = Vector3.Project(w, N);

			distance = projection.magnitude;
			distance *= (projection.normalized == N.normalized) ? 1.0f : -1.0f;
			distance *= (hand.IsRight) ? 1.0f : -1.0f;

			if (distance > 0)
			{
				return 'A';
			}
		}

		// Test for 'O'
		float theO_Threshold = 40;
		Leap.Finger middle = fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE];
		Vector3 middlePos = ToVector3(middle.TipPosition);
		Vector3 thumbPos = ToVector3(thumb.TipPosition);
		float dist = Vector3.Distance(middlePos, thumbPos);

		if (dist < theO_Threshold)
		{
			return 'O';
		}
		else
		{
			// Otherwise, not worth getting the rest. . .
			return INVALID_ASL_LETTER;
		}

		/*
		Vector3 pinkyPos = ToVector3(pinky.Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).NextJoint);
		Vector3 ringPos = ToVector3(ring.Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).NextJoint);
		Vector3 middlePos = ToVector3(middle.Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).NextJoint);
		Vector3 indexPos = ToVector3(index.Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).NextJoint);
		Vector3 thumbPos = ToVector3(thumb.TipPosition);
		Vector3 palmPos = ToVector3(hand.PalmPosition);

		Vector3 middleToThumb = thumbPos - middlePos;
		Vector3 indexToThumb = thumbPos - indexPos;
		Vector3 indexToMiddle = middlePos - indexPos;

		float middleToThumbDist = middleToThumb.sqrMagnitude;

		if (AreTwoFingersInContact(MyFingerType.TYPE_PINKY, MyFingerType.TYPE_THUMB, hand, pinkyThreshold))
		{
			return 'E';
		}

		// Check Based on distance from Bone
		float pinkyToThumbDist = Vector3.Distance(pinkyPos, thumbPos);
		float ringToThumbDist = Vector3.Distance(ringPos, thumbPos);
		middleToThumbDist = Mathf.Sqrt(middleToThumbDist);
		float indexToThumbDist = indexToThumb.magnitude;

		// Test for what the thumb is closest to
		MyFingerType fingerClosest = MyFingerType.TYPE_PINKY;
		float currentClosest = pinkyToThumbDist;
		if (ringToThumbDist < currentClosest)
		{
			fingerClosest = MyFingerType.TYPE_RING;
			currentClosest = ringToThumbDist;
		}
		if (middleToThumbDist < currentClosest)
		{
			fingerClosest = MyFingerType.TYPE_MIDDLE;
			currentClosest = middleToThumbDist;
		}
		if (indexToThumbDist < currentClosest)
		{
			fingerClosest = MyFingerType.TYPE_INDEX;
			currentClosest = indexToThumbDist;
		}

		// Near Pinky = E
		// Near Ring = M
		// Near Middle = N, S
		// Near Index = T, A
		if (currentClosest < closestFingerThreshold)
		{
			switch (fingerClosest)
			{
				case MyFingerType.TYPE_PINKY:	return 'E';
				case MyFingerType.TYPE_RING:
				case MyFingerType.TYPE_MIDDLE:
				case MyFingerType.TYPE_INDEX:	return 'O';
			}
		}
		return INVALID_ASL_LETTER;*/

	}

	private char DoASLTest_B(Leap.Hand hand)
	{
		float fingerThreshold = 1400;
		Leap.FingerList fingers = hand.Fingers;

		Leap.Finger pinky = fingers[(int)Leap.Finger.FingerType.TYPE_PINKY];
		Leap.Finger ring = fingers[(int)Leap.Finger.FingerType.TYPE_RING];
		Leap.Finger middle = fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE];
		Leap.Finger index = fingers[(int)Leap.Finger.FingerType.TYPE_INDEX];
		Leap.Finger thumb = fingers[(int)Leap.Finger.FingerType.TYPE_THUMB];

		Vector3 pinkyPos = ToVector3(pinky.Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).NextJoint);
		Vector3 ringPos = ToVector3(ring.Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).NextJoint);
		Vector3 middlePos = ToVector3(middle.Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).NextJoint);
		Vector3 indexPos = ToVector3(index.Bone(Leap.Bone.BoneType.TYPE_INTERMEDIATE).NextJoint);

		if ((pinkyPos - ringPos).sqrMagnitude < fingerThreshold
		&& (ringPos - middlePos).sqrMagnitude < fingerThreshold
		&& (middlePos - indexPos).sqrMagnitude < fingerThreshold)
		{
			return 'B';
		}
		return INVALID_ASL_LETTER;
	}

	private char DoASLTest_CO(Leap.Hand hand)
	{
		float threshold = 40;

		Leap.FingerList fingers = hand.Fingers;
		Leap.Finger middle = fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE];
		Leap.Finger thumb = fingers[(int)Leap.Finger.FingerType.TYPE_THUMB];
		Vector3 middlePos = ToVector3(middle.TipPosition);
		Vector3 thumbPos = ToVector3(thumb.TipPosition);

		float dist = Vector3.Distance(middlePos, thumbPos);
		//Debug.Log("hey " + dist);

		return (dist < threshold) ? 'O' : 'C';
	}

	private char DoASLTest_DGPQZ(Leap.Hand hand)
	{
		float angleThreshold = 30;

		Vector3 palmDirection = ToVector3(hand.Direction);
		Vector3 palmNormal = ToVector3(hand.PalmNormal);
		Vector3 sidePalmFacing = Vector3.Cross(palmNormal, palmDirection);
		//Debug.DrawRay(ToVector3(hand.PalmPosition), sidePalmFacing * 1000);

		float angleTestG = Vector3.Angle((hand.IsLeft) ? Vector3.up : Vector3.down, sidePalmFacing);

		//Debug.Log((sidePalmFacing.normalized * 100) + " " + angleToUpVector);
		if (angleTestG < angleThreshold)
		{
			return 'G';
		}


		float angleTestPQ = Vector3.Angle(Vector3.down, palmNormal);
		if (angleTestPQ < angleThreshold)
		{
			return 'P';
		}

		float angleTestD = Vector3.Angle(Vector3.up, palmDirection);
		if (angleTestD < 1.5 * angleThreshold)
		{
			return 'D';
		}

		if (angleTestD > 90 && angleTestPQ < 1.5 * angleThreshold)
		{
			return 'Q';
		}


		return INVALID_ASL_LETTER;
	}

	private char DoASLTest_F(Leap.Hand hand)
	{
		// Sensitivity for two fingers good enough
		if (AreTwoFingersInContact(MyFingerType.TYPE_INDEX, MyFingerType.TYPE_THUMB, hand, 30))
		{
			return 'F';
		}
		else
		{
			return INVALID_ASL_LETTER;
		}
	}

	private char DoASLTest_HKRUV(Leap.Hand hand)
	{
		float equalDistanceThreshold = 7;
		float fingerPointThreshold = 12;
		float fingersApartThreshold = 25;
		float angleThreshold = 45;

		Leap.FingerList fingers = hand.Fingers;
		Leap.Finger index = fingers[(int)Leap.Finger.FingerType.TYPE_INDEX];
		Leap.Finger middle = fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE];
		Leap.Finger ring = fingers[(int)Leap.Finger.FingerType.TYPE_RING];
		Leap.Finger thumb = fingers[(int)Leap.Finger.FingerType.TYPE_THUMB];

		Vector3 indexPos = ToVector3(index.Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).Center);
		Vector3 middlePos = ToVector3(middle.Bone(Leap.Bone.BoneType.TYPE_PROXIMAL).Center);
		Vector3 thumbPos = ToVector3(thumb.StabilizedTipPosition);
		float thumbToIndex = Vector3.Distance(indexPos, thumbPos);
		float thumbToMiddle = Vector3.Distance(middlePos, thumbPos);

		Vector3 indexDir = ToVector3(index.Direction);
		Vector3 middleDir = ToVector3(middle.Direction);
		Vector3 thumbDir = ToVector3(thumb.Direction);

		Vector3 palmDirection = ToVector3(hand.Direction);
		Vector3 palmNormal = ToVector3(hand.PalmNormal);
		Vector3 sidePalmFacing = Vector3.Cross(palmNormal, palmDirection);
		float angleTestH = Vector3.Angle((hand.IsLeft) ? Vector3.up : Vector3.down, sidePalmFacing);
		float indexAngle = Vector3.Angle(middleDir, indexDir);
		float thumbAngle = Vector3.Angle(Vector3.up, thumbDir);

		Debug.Log(thumbAngle + " " + indexAngle + " (" + thumbToIndex + ", " + thumbToMiddle + ")");

		// HRU test - Index and Middle are touching
		if (AreTwoFingersInContact(MyFingerType.TYPE_INDEX, MyFingerType.TYPE_MIDDLE, hand, fingersApartThreshold))
		{
			// H Test - Hand is tilted
			if (angleTestH < angleThreshold)
			{
				return 'H';
			}

			// R Test - Index crosses Middle
			else if (indexAngle > fingerPointThreshold)
			{
				return 'R';
			}

			// Otherwise, it must be U
			else
			{
				return 'U';
			}
		}

		// K Test - Index apart from Middle, and Thumb equidistant to Index and Middle
		//else if (thumbAngle < angleThreshold * 0.5f)
		else if (Mathf.Abs(thumbToIndex - thumbToMiddle) < equalDistanceThreshold)
		{
			return 'K';
		}

		// V Test - Index apart from Middle, Thumb not pointing up
		//          *Note, Thumb is not UP according to First-Pass
		return 'V';
	}

	private char DoASLTest_IJ(Leap.Hand hand)
	{
		// No 'J' will be tested
		return 'I';
	}

	private char DoASLTest_L(Leap.Hand hand)
	{
		return 'L';
	}
	/*
	private char DoASLTest_PQ(Leap.Hand hand)
    {
        float fingerNearThreshold = 15;
        float fingerFarThreshold = 30;
        
        Leap.FingerList fingers = hand.Fingers;
        Leap.Finger index = fingers[(int)Leap.Finger.FingerType.TYPE_INDEX];
        Leap.Finger middle = fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE];
        Leap.Finger thumb = fingers[(int)Leap.Finger.FingerType.TYPE_THUMB];

        Vector3 indexPos = ToVector3(index.TipPosition);
        Vector3 middlePos = ToVector3(middle.TipPosition);
        Vector3 thumbPos = ToVector3(thumb.TipPosition);

        float indexToThumbDist = Vector3.Distance(indexPos, thumbPos);
        float middleToThumbDist = Vector3.Distance(middlePos, thumbPos);

        Debug.Log("hey " + indexToThumbDist + " / " + middleToThumbDist);

        // Q test
        if (indexToThumbDist > fingerNearThreshold && indexToThumbDist < fingerFarThreshold)
        {
            return 'Q';
        }

        // P Test
        else if (indexToThumbDist > fingerNearThreshold
            && AreTwoFingersInContact(MyFingerType.TYPE_MIDDLE, MyFingerType.TYPE_THUMB, hand, fingerNearThreshold))
        {
            return 'P';
        }

        // Not a close enough match to either
        return INVALID_ASL_LETTER;
    }*/

	private char DoASLTest_W(Leap.Hand hand)
	{
		// Would be doing two finger connectivity if Leap Motion proved accurate enough...
		return 'W';
	}

	private char DoASLTest_X(Leap.Hand hand)
	{
		return 'X';
	}

	private char DoASLTest_Y(Leap.Hand hand)
	{
		return 'Y';
	}

}   /* ================================ End of InputManager =================================== */
