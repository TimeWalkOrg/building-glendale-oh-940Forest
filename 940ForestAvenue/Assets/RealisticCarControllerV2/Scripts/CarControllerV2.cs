using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent (typeof (Rigidbody))]

public class CarControllerV2 : MonoBehaviour {
	
	//Mobile Controller
	public bool mobileController = false;
	public bool useAccelerometerForSteer = false, steeringWheelControl = false;
	private Vector3 defBrakePedalPosition;
	public bool demoGUI = false;
	public bool dashBoard = false;
	private bool andHandBrake = false;
	public float gyroTiltMultiplier = 2f;
	public GUITexture gasPedal, brakePedal, leftArrow, rightArrow, handBrakeGui;
	
	//Wheel colliders of the vehicle.
	public WheelCollider Wheel_FL;
	public WheelCollider Wheel_FR;
	public WheelCollider Wheel_RL;
	public WheelCollider Wheel_RR;
	
	// Wheel transforms of the vehicle.
	public Transform FrontLeftWheelT;
	public Transform FrontRightWheelT;
	public Transform RearLeftWheelT;
	public Transform RearRightWheelT;
	
	public WheelCollider[] ExtraRearWheels;
	public Transform[] ExtraRearWheelsT;
	
	// Driver Steering Wheel.
	public Transform SteeringWheel;
	
	// Set wheel drive of the vehicle. If you are using rwd, you have to be careful with your rear wheel collider
	// settings and com of the vehicle. Otherwise, vehicle will behave like a toy. ***My advice is use fwd always***
	public enum WheelType{FWD, RWD};
	public WheelType _wheelTypeChoise;
	private bool rwd = false, fwd = true;
	
	//Center of mass.
	public Transform COM;
	
	// Drift Configurations
	public int steeringAssistanceDivider = 5;
	private float driftAngle;
	private float stabilizerAssistance = 500f;
	
	//Vehicle Mecanim
	public bool canControl = true;
	public bool canBurnout = true;
	public bool driftMode = false;
	public float gearShiftRate = 10.0f;
	public int CurrentGear;
	public AnimationCurve EngineTorqueCurve;
	private float[] GearRatio;
	public float EngineTorque = 600.0f;
	public float MaxEngineRPM = 6000.0f;
	public float MinEngineRPM = 1000.0f;
	public float SteerAngle = 20.0f;
	private float LowestSpeedSteerAngleAtSpeed = 40.0f;
	public float HighSpeedSteerAngle = 10.0f;
	public float HighSpeedSteerAngleAtSpeed = 80.0f;
	[HideInInspector]
	public float Speed;
	public float Brake = 200.0f;
	public float handbrakeStiffness = 0.1f;
	public float maxSpeed = 180.0f;
	public bool useDifferantial = true;
	private float differantialRatioRight;
	private float differantialRatioLeft;
	private float differantialDifference;
	
	// Each wheel transform's rotation value.
	private float RotationValueFL, RotationValueFR, RotationValueRL, RotationValueRR;
	private float[] RotationValueExtra;

	//Misc.
	private float defSteerAngle;
	private float StiffnessRear;
	private float StiffnessFront;
	private bool reversing = false;
	private bool centerSteer = false;
	private bool headLightsOn = false;
	private float acceleration = 0f;
	private float lastVelocity = 0f;
	private float gearTimeMultiplier;
	
	//Audio.
	private GameObject skidAudio;
	public AudioClip skidClip;
	private GameObject crashAudio;
	public AudioClip[] crashClips;
	private GameObject engineAudio;
	public AudioClip engineClip;
	
	//Collision Force Limit.
	private int collisionForceLimit = 5;
	
	//Inputs.
	private float EngineRPM;
	[HideInInspector]
	public float motorInput;
	[HideInInspector]
	public float steerInput;
	
	//DashBoard.
	public Texture2D speedOMeter;
	public Texture2D speedOMeterNeedle;
	public Texture2D kiloMeter;
	public Texture2D kiloMeterNeedle;
	private float needleRotation = 0.0f;
	private float kMHneedleRotation = 0.0f;
	private float smoothedNeedleRotation = 0.0f;
	public Font dashBoardFont;
	public float guiWidth = 0.0f;
	public float guiHeight = 0.0f;
	
	//Smokes.
	public GameObject WheelSlipPrefab;
	private List <GameObject> WheelParticles = new List<GameObject>();
	public ParticleEmitter normalExhaustGas;
	public ParticleEmitter heavyExhaustGas;
	
	//Sideways Frictions.
	private WheelFrictionCurve RearLeftFriction;
	private WheelFrictionCurve RearRightFriction;
	private WheelFrictionCurve FrontLeftFriction;
	private WheelFrictionCurve FrontRightFriction;
	
	//Chassis Simulation.
	public GameObject chassis;
	public float chassisVerticalLean = 3.0f;
	public float chassisHorizontalLean = 3.0f;
	private float horizontalLean = 0.0f;
	private float verticalLean = 0.0f;
	
	//Lights.
	public Light[] HeadLights;
	public Light[] BrakeLights;
	public Light[] ReverseLights;
	
	//Steering Wheel Controller
	public float steeringWheelMaximumSteerAngle = 180f;
	public float steeringWheelGuiScale = 256f;
	public float steeringWheelXOffset = 30;
	public float steeringWheelYOffset = 30;
	public Vector2 steeringWheelPivotPos = Vector2.zero;
	public float steeringWheelResetPosSpeed = 200f;
	public Texture2D steeringWheelTexture;
	private float steeringWheelsteerAngle ;
	private bool  steeringWheelIsTouching;
	private Rect steeringWheelTextureRect;
	private Vector2 steeringWheelWheelCenter;
	private float steeringWheelOldAngle;
	private int touchId = -1;
	private Vector2 touchPos;
	
	
	
	void Start (){
		
		SetWheelFrictions();
		SoundsInitialize();
		WheelTypeInit();
		GearInit();
		MobileGUI();
		SteeringWheelInit();
		if(WheelSlipPrefab)
			SmokeInit();
		
		Time.fixedDeltaTime = .03f;
		GetComponent<Rigidbody>().centerOfMass = new Vector3(COM.localPosition.x * transform.localScale.x , COM.localPosition.y * transform.localScale.y , COM.localPosition.z * transform.localScale.z);
		GetComponent<Rigidbody>().maxAngularVelocity = 5f;
		guiWidth = Screen.width/2 - 200;
		
		if(mobileController)
			defBrakePedalPosition = brakePedal.transform.position;
		
	}
	
	void SetWheelFrictions(){
		
		RearLeftFriction = Wheel_RL.sidewaysFriction;
		RearRightFriction = Wheel_RR.sidewaysFriction;
		FrontLeftFriction = Wheel_FL.sidewaysFriction;
		FrontRightFriction = Wheel_FR.sidewaysFriction;

		RotationValueExtra = new float[ExtraRearWheels.Length];
		
		StiffnessRear = Wheel_RL.sidewaysFriction.stiffness;
		StiffnessFront = Wheel_FL.sidewaysFriction.stiffness;
		defSteerAngle = SteerAngle;
		
	}
	
	void SoundsInitialize(){
		
		engineAudio = new GameObject("EngineSound");
		engineAudio.transform.position = transform.position;
		engineAudio.transform.rotation = transform.rotation;
		engineAudio.transform.parent = transform;
		engineAudio.AddComponent<AudioSource>();
		engineAudio.GetComponent<AudioSource>().minDistance = 5;
		engineAudio.GetComponent<AudioSource>().volume = 0;
		engineAudio.GetComponent<AudioSource>().clip = engineClip;
		engineAudio.GetComponent<AudioSource>().loop = true;
		engineAudio.GetComponent<AudioSource>().Play();
		
		skidAudio = new GameObject("SkidSound");
		skidAudio.transform.position = transform.position;
		skidAudio.transform.rotation = transform.rotation;
		skidAudio.transform.parent = transform;
		skidAudio.AddComponent<AudioSource>();
		skidAudio.GetComponent<AudioSource>().minDistance = 10;
		skidAudio.GetComponent<AudioSource>().volume = 0;
		skidAudio.GetComponent<AudioSource>().clip = skidClip;
		skidAudio.GetComponent<AudioSource>().loop = true;
		skidAudio.GetComponent<AudioSource>().Play();
		
		crashAudio = new GameObject("CrashSound");
		crashAudio.transform.position = transform.position;
		crashAudio.transform.rotation = transform.rotation;
		crashAudio.transform.parent = transform;
		crashAudio.AddComponent<AudioSource>();
		crashAudio.GetComponent<AudioSource>().minDistance = 10;
		
	}
	
	void WheelTypeInit(){
		
		switch(_wheelTypeChoise){
		case WheelType.FWD:
			fwd = true;
			rwd = false;
			break;
		case WheelType.RWD:
			fwd = false;
			rwd = true;
			break;
		}
		
	}
	
	void GearInit(){
		
		GearRatio = new float[EngineTorqueCurve.length];
		
		for(int i = 0; i < EngineTorqueCurve.length; i++){
			
			GearRatio[i] = EngineTorqueCurve.keys[i].value;
			
		}
		
	}

	void Differantial(){
		
		if(useDifferantial){
			
			differantialDifference = Mathf.Clamp ( Mathf.Abs (Wheel_RR.rpm) - Mathf.Abs (Wheel_RL.rpm), -100f, 100f );
			differantialRatioRight = Mathf.Lerp ( 0f, 1f, ( (((Wheel_RR.rpm + Wheel_RL.rpm) + 10 / 2 ) + differantialDifference) /  (Wheel_RR.rpm + Wheel_RL.rpm)) );
			differantialRatioLeft = Mathf.Lerp ( 0f, 1f, ( (((Wheel_RR.rpm + Wheel_RL.rpm) + 10 / 2 ) - differantialDifference) /  (Wheel_RR.rpm + Wheel_RL.rpm)) );
			
		}else{
			
			differantialRatioRight = 1;
			differantialRatioLeft = 1;
			
		}
		
	}
	
	void MobileGUI(){
		
		if(!mobileController){
			if(gasPedal)
				gasPedal.gameObject.SetActive(false);
			if(brakePedal)
				brakePedal.gameObject.SetActive(false);
			if(leftArrow)
				leftArrow.gameObject.SetActive(false);
			if(rightArrow)
				rightArrow.gameObject.SetActive(false);
			if(handBrakeGui)
				handBrakeGui.gameObject.SetActive(false);
		}
		
	}
	
	void SteeringWheelInit(){
		
		steeringWheelGuiScale = (Screen.width * 1.0f) / 2.7f;
		steeringWheelIsTouching = false;
		steeringWheelTextureRect = new Rect( steeringWheelXOffset + (steeringWheelGuiScale / Screen.width ), -steeringWheelYOffset + (Screen.height - (steeringWheelGuiScale)), steeringWheelGuiScale, steeringWheelGuiScale );
		steeringWheelWheelCenter = new Vector2( steeringWheelTextureRect.x + steeringWheelTextureRect.width * 0.5f, Screen.height - steeringWheelTextureRect.y - steeringWheelTextureRect.height * 0.5f );
		steeringWheelsteerAngle  = 0f;
		
	}
	
	void SmokeInit(){
		
		Instantiate(WheelSlipPrefab, Wheel_FR.transform.position, transform.rotation);
		Instantiate(WheelSlipPrefab, Wheel_FL.transform.position, transform.rotation);
		Instantiate(WheelSlipPrefab, Wheel_RR.transform.position, transform.rotation);
		Instantiate(WheelSlipPrefab, Wheel_RL.transform.position, transform.rotation);
		
		foreach(GameObject go in GameObject.FindObjectsOfType(typeof(GameObject)))
		{
			if(go.name == "WheelSlip(Clone)")
				WheelParticles.Add (go);
		}
		
		WheelParticles[0].transform.position = Wheel_FR.transform.position;
		WheelParticles[1].transform.position = Wheel_FL.transform.position;
		WheelParticles[2].transform.position = Wheel_RR.transform.position;
		WheelParticles[3].transform.position = Wheel_RL.transform.position;
		
		WheelParticles[0].transform.parent = Wheel_FR.transform;
		WheelParticles[1].transform.parent = Wheel_FL.transform;
		WheelParticles[2].transform.parent = Wheel_RR.transform;
		WheelParticles[3].transform.parent = Wheel_RL.transform;
		
	}
	
	void Update (){
		
		WheelAlign();
		
		if(canControl){
			Lights();
			
			if(mobileController)
				TouchScreenControlling();
			
			if(chassis)
				Chassis();
		}
		
	}
	
	void FixedUpdate(){
		
		ShiftGears();
		SkidAudio();
		Braking();
		Differantial();

		if(WheelSlipPrefab)
			SmokeInstantiateRate();

		if(canControl){
			Engine();
			if(mobileController){
				MobileSteeringInputs();
				if(steeringWheelControl)
					SteeringWheelControlling();
			}else{
				KeyboardControlling();
			}

		}
		
	}
	
	void Engine(){
		
		//Engine Curve
		if(EngineTorqueCurve.keys.Length >= 2){
			if(CurrentGear == EngineTorqueCurve.length-2) gearTimeMultiplier = (((-EngineTorqueCurve[CurrentGear].time / gearShiftRate) / (maxSpeed * 3)) + 1f); else gearTimeMultiplier = ((-EngineTorqueCurve[CurrentGear].time / (maxSpeed * 3)) + 1f);
		}else{
			gearTimeMultiplier = 1;
			Debug.Log ("You DID NOT CREATE any engine torque curve keys!, Please create 1 key at least...");
		}
		
		//Speed
		Speed = GetComponent<Rigidbody>().velocity.magnitude * 3.0f;
		//Stabilizer
		GetComponent<Rigidbody>().AddRelativeTorque(Vector3.up * ((motorInput * steerInput) * (stabilizerAssistance * 10f)));
		
		//Acceleration Calculation.
		acceleration = 0f;
		acceleration = (transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity).z - lastVelocity) / Time.fixedDeltaTime;
		lastVelocity = transform.InverseTransformDirection(GetComponent<Rigidbody>().velocity).z;
		
		//Drag Limit.
		if(Speed < 100)
			GetComponent<Rigidbody>().drag = Mathf.Clamp((acceleration / 30), 0f, 1f);
		else 
			GetComponent<Rigidbody>().drag = .04f;
		
		//Steer Limit.
		if(Speed > HighSpeedSteerAngleAtSpeed){
			SteerAngle = Mathf.Lerp(SteerAngle, HighSpeedSteerAngle, Time.deltaTime*5);
		}else if(Speed < LowestSpeedSteerAngleAtSpeed){
			SteerAngle = Mathf.Lerp(SteerAngle, defSteerAngle * 1.5f, Time.deltaTime*5);
		}else{
			SteerAngle = Mathf.Lerp(SteerAngle, defSteerAngle, Time.deltaTime*5);
		}
		
		//Engine RPM.
		if(EngineTorqueCurve.keys.Length >= 2){
			EngineRPM = ((((Mathf.Abs ((Wheel_FR.rpm * gearShiftRate * Mathf.Clamp01(motorInput)) + (Wheel_FL.rpm * gearShiftRate * motorInput)) / 2 ) * (GearRatio[CurrentGear])) * gearTimeMultiplier ) + MinEngineRPM);
		}else{
			EngineRPM = ((((Mathf.Abs ((Wheel_FR.rpm * gearShiftRate * Mathf.Clamp01(motorInput)) + (Wheel_FL.rpm * gearShiftRate * motorInput)) / 2 )) * gearTimeMultiplier ) + MinEngineRPM);
		}
		
		//Reversing Bool.
		if(motorInput < 0  && Wheel_FL.rpm < 50)
			reversing = true;
		else reversing = false;
		
		//Engine Audio Volume.
		engineAudio.GetComponent<AudioSource>().volume = Mathf.Lerp (engineAudio.GetComponent<AudioSource>().volume, Mathf.Clamp (motorInput, .25f, 1.0f), Time.deltaTime*5);
		
		if(Speed < 40 && !reversing && canBurnout){
			engineAudio.GetComponent<AudioSource>().pitch = Mathf.Lerp (engineAudio.GetComponent<AudioSource>().pitch, Mathf.Clamp(motorInput * 2, 1f, 2f), Time.deltaTime * 5);
			skidAudio.GetComponent<AudioSource>().volume = Mathf.Lerp (skidAudio.GetComponent<AudioSource>().volume, Mathf.Clamp(motorInput, 0f, 1f), Time.deltaTime * 5);
		}else if(Speed > 5){
				engineAudio.GetComponent<AudioSource>().pitch = Mathf.Lerp ( engineAudio.GetComponent<AudioSource>().pitch, Mathf.Lerp (1f, 2f, (EngineRPM - MinEngineRPM / 1.5f) / (MaxEngineRPM + MinEngineRPM)), Time.deltaTime * 5 );
		}else{
				engineAudio.GetComponent<AudioSource>().pitch = Mathf.Lerp (engineAudio.GetComponent<AudioSource>().pitch, Mathf.Clamp(motorInput * 2, 1f, 2f), Time.deltaTime * 5);
		}
		
		//Applying Torque.
		if(rwd){
			
			if(Speed > maxSpeed){
				Wheel_RL.motorTorque = 0;
				Wheel_RR.motorTorque = 0;
			}else if(!reversing){
				Wheel_RL.motorTorque = EngineTorque  * Mathf.Clamp(motorInput * differantialRatioLeft, 0f, 1f) * EngineTorqueCurve.Evaluate(Speed);
				Wheel_RR.motorTorque = EngineTorque  * Mathf.Clamp(motorInput * differantialRatioRight, 0f, 1f) * EngineTorqueCurve.Evaluate(Speed);
			}
			if(reversing){
				if(Speed < 30){
					Wheel_RL.motorTorque = (EngineTorque  * motorInput) / 3;
					Wheel_RR.motorTorque = (EngineTorque  * motorInput) / 3;
				}else{
					Wheel_RL.motorTorque = 0;
					Wheel_RR.motorTorque = 0;
				}
			}
			
		}
		
		if(fwd){
			
			if(Speed > maxSpeed){
				Wheel_FL.motorTorque = 0;
				Wheel_FR.motorTorque = 0;
			}else if(!reversing){
				Wheel_FL.motorTorque = EngineTorque  * Mathf.Clamp(motorInput * differantialRatioLeft, 0f, 1f) * EngineTorqueCurve.Evaluate(Speed);
				Wheel_FR.motorTorque = EngineTorque  * Mathf.Clamp(motorInput * differantialRatioRight, 0f, 1f) * EngineTorqueCurve.Evaluate(Speed);
			}
			if(reversing){
				if(Speed < 30){
					Wheel_FL.motorTorque = (EngineTorque  * motorInput) / 3;
					Wheel_FR.motorTorque = (EngineTorque  * motorInput) / 3;
				}else{
					Wheel_FL.motorTorque = 0;
					Wheel_FR.motorTorque = 0;
				}
			}	
		}
		
	}
	
	void MobileSteeringInputs(){
		
		if(useAccelerometerForSteer){
			
			steerInput = Input.acceleration.x * gyroTiltMultiplier;
			//Accelerometer Inputs.
			if(!driftMode){
				Wheel_FL.steerAngle = Mathf.Clamp((SteerAngle * steerInput), -SteerAngle, SteerAngle);
				Wheel_FR.steerAngle = Mathf.Clamp((SteerAngle * steerInput), -SteerAngle, SteerAngle);
			}else{
				Wheel_FL.steerAngle = Mathf.Clamp((SteerAngle * steerInput), -SteerAngle, SteerAngle) + (driftAngle / steeringAssistanceDivider);
				Wheel_FR.steerAngle = Mathf.Clamp((SteerAngle * steerInput), -SteerAngle, SteerAngle) + (driftAngle / steeringAssistanceDivider);
			}
			
		}else{
			
			if(!steeringWheelControl){
				//TouchScreen Inputs.
				if(!driftMode){
					Wheel_FL.steerAngle = Mathf.Clamp((SteerAngle * steerInput), -SteerAngle, SteerAngle);
					Wheel_FR.steerAngle = Mathf.Clamp((SteerAngle * steerInput), -SteerAngle, SteerAngle);
				}else{
					Wheel_FL.steerAngle = Mathf.Clamp((SteerAngle * steerInput), -SteerAngle, SteerAngle) + (driftAngle / steeringAssistanceDivider);
					Wheel_FR.steerAngle = Mathf.Clamp((SteerAngle * steerInput), -SteerAngle, SteerAngle) + (driftAngle / steeringAssistanceDivider);
				}
				
			}else{
				//SteeringWheel Inputs.
				if(!driftMode){
					Wheel_FL.steerAngle = (SteerAngle * (-steeringWheelsteerAngle / steeringWheelMaximumSteerAngle));
					Wheel_FR.steerAngle = (SteerAngle * (-steeringWheelsteerAngle / steeringWheelMaximumSteerAngle));
				}else{
					Wheel_FL.steerAngle = (SteerAngle * (-steeringWheelsteerAngle / steeringWheelMaximumSteerAngle)) + (driftAngle / steeringAssistanceDivider);
					Wheel_FR.steerAngle = (SteerAngle * (-steeringWheelsteerAngle / steeringWheelMaximumSteerAngle)) + (driftAngle / steeringAssistanceDivider);
				}
				
			}
			
		}
		
	}
	
	void SteeringWheelControlling(){
		
		if( steeringWheelIsTouching ){
			
			foreach( Touch touch in Input.touches )
			{
				if( touch.fingerId == touchId ){
					touchPos = touch.position;
					
					if( touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled ){
						steeringWheelIsTouching = false; 
					}
				}
			}
			
			float newSteerAngle = Vector2.Angle( Vector2.up, touchPos - steeringWheelWheelCenter );
			
			if( Vector2.Distance( touchPos, steeringWheelWheelCenter ) > 20f ){
				if( touchPos.x > steeringWheelWheelCenter.x )
					steeringWheelsteerAngle  -= newSteerAngle - steeringWheelOldAngle;
				else
					steeringWheelsteerAngle  += newSteerAngle - steeringWheelOldAngle;
			}
			
			if( steeringWheelsteerAngle  > steeringWheelMaximumSteerAngle )
				steeringWheelsteerAngle  = steeringWheelMaximumSteerAngle;
			else if( steeringWheelsteerAngle  < -steeringWheelMaximumSteerAngle )
				steeringWheelsteerAngle  = -steeringWheelMaximumSteerAngle;
			
			steeringWheelOldAngle = newSteerAngle;
		}else{
			
			foreach( Touch touch in Input.touches ){
				if( touch.phase == TouchPhase.Began ){
					if( steeringWheelTextureRect.Contains( new Vector2( touch.position.x, Screen.height - touch.position.y ) ) ){
						steeringWheelIsTouching = true;
						steeringWheelOldAngle = Vector2.Angle( Vector2.up, touch.position - steeringWheelWheelCenter );
						touchId = touch.fingerId;
					}
				}
			}
			
			if( !Mathf.Approximately( 0f, steeringWheelsteerAngle  ) ){
				float deltaAngle = steeringWheelResetPosSpeed * Time.deltaTime;
				
				if( Mathf.Abs( deltaAngle ) > Mathf.Abs( steeringWheelsteerAngle  ) ){
					steeringWheelsteerAngle  = 0f;
					return;
				}
				
				if( steeringWheelsteerAngle  > 0f )
					steeringWheelsteerAngle  -= deltaAngle;
				else
					steeringWheelsteerAngle  += deltaAngle;
			}
		}
		
	}
	
	void TouchScreenControlling(){
		
		if(centerSteer && !steeringWheelControl && !useAccelerometerForSteer){
			
			if(steerInput*10 < -1)
				steerInput += Time.deltaTime*1.5f;
			if(steerInput*10 > 1)
				steerInput -= Time.deltaTime*1.5f;
			
			if(steerInput*10 > -2 && steerInput*10 < 2)
				steerInput = 0;
			
		}
		
		for(int i = 0; i < Input.touchCount; i++){
			
			if( Input.GetTouch(i).phase != TouchPhase.Ended && gasPedal.HitTest(Input.GetTouch(i).position )){
				motorInput = Mathf.Lerp(motorInput, 1, Time.deltaTime*10);
			}else if(  Input.GetTouch(i).phase == TouchPhase.Ended && gasPedal.HitTest(Input.GetTouch(i).position)){
				motorInput = 0;
			}
			
			if( Input.GetTouch(i).phase != TouchPhase.Ended && brakePedal.HitTest( Input.GetTouch(i).position )){
				motorInput = Mathf.Lerp(motorInput, -1, Time.deltaTime*10);
			}else if( Input.GetTouch(i).phase == TouchPhase.Ended && brakePedal.HitTest(Input.GetTouch(i).position)){
				motorInput = 0;
			}
			
			if( !useAccelerometerForSteer && !steeringWheelControl && Input.GetTouch(i).phase != TouchPhase.Ended && leftArrow.HitTest( Input.GetTouch(i).position )){
				if(Mathf.Abs(steerInput) < 1)
					steerInput -= Time.deltaTime * 1.5f;
				centerSteer = false;
				print(steerInput);
			}
			
			else if( !useAccelerometerForSteer && !steeringWheelControl && Input.GetTouch(i).phase == TouchPhase.Ended && leftArrow.HitTest( Input.GetTouch(i).position )){
				centerSteer = true;
			}
			
			if( !useAccelerometerForSteer && !steeringWheelControl && Input.GetTouch(i).phase != TouchPhase.Ended && rightArrow.HitTest( Input.GetTouch(i).position )){
				if(Mathf.Abs(steerInput) < 1)
					steerInput += Time.deltaTime * 1.5f;
				centerSteer = false;
			}
			
			else if( !useAccelerometerForSteer && !steeringWheelControl && Input.GetTouch(i).phase == TouchPhase.Ended && rightArrow.HitTest( Input.GetTouch(i).position )){
				centerSteer = true;
			}
			
			if( Input.GetTouch(i).phase != TouchPhase.Ended && handBrakeGui.HitTest( Input.GetTouch(i).position )){
				andHandBrake = true;
			}else if( Input.GetTouch(i).phase == TouchPhase.Ended && handBrakeGui.HitTest( Input.GetTouch(i).position )){
				andHandBrake = false;
			}
			
		}
		
	}
	
	void KeyboardControlling(){
		
		motorInput = Input.GetAxis("Vertical");
		steerInput = Input.GetAxis("Horizontal");
		
		//Keyboard Inputs.
		Wheel_FL.steerAngle = (SteerAngle * steerInput) + (driftAngle / steeringAssistanceDivider);
		Wheel_FR.steerAngle = (SteerAngle * steerInput) + (driftAngle / steeringAssistanceDivider);
		
	}
	
	void ShiftGears(){
		
		for(int i = 0; i < EngineTorqueCurve.length; i++){
			
			if(EngineTorqueCurve.Evaluate(Speed) < EngineTorqueCurve.keys[i].value)
				CurrentGear = i;
			
		}
		
	}
	
	void  WheelAlign (){
		
		RaycastHit hit;
		WheelHit CorrespondingGroundHit;
		
		
		//Front Left Wheel Transform.
		Vector3 ColliderCenterPointFL = Wheel_FL.transform.TransformPoint( Wheel_FL.center );
		Wheel_FL.GetGroundHit( out CorrespondingGroundHit );
		
		if ( Physics.Raycast( ColliderCenterPointFL, -Wheel_FL.transform.up, out hit, (Wheel_FL.suspensionDistance + Wheel_FL.radius) * transform.localScale.y) ) {
			if(hit.transform.gameObject.layer != LayerMask.NameToLayer("CarCollider")){
				FrontLeftWheelT.transform.position = hit.point + (Wheel_FL.transform.up * Wheel_FL.radius) * transform.localScale.y;
				float extension = (-Wheel_FL.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - Wheel_FL.radius) / Wheel_FL.suspensionDistance;
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + Wheel_FL.transform.up * (CorrespondingGroundHit.force / 8000), extension <= 0.0 ? Color.magenta : Color.white);
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_FL.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_FL.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
			}
		}else{
			FrontLeftWheelT.transform.position = ColliderCenterPointFL - (Wheel_FL.transform.up * Wheel_FL.suspensionDistance) * transform.localScale.y;
		}
		
		if(fwd && Speed < 20 && motorInput > 0 && canBurnout) 
			RotationValueFL += Wheel_FL.rpm * ( 12 ) * Time.deltaTime;
		else
			RotationValueFL += Wheel_FL.rpm * ( 6 ) * Time.deltaTime;
		
		FrontLeftWheelT.transform.rotation = Wheel_FL.transform.rotation * Quaternion.Euler( RotationValueFL, Wheel_FL.steerAngle + (driftAngle / steeringAssistanceDivider), Wheel_FL.transform.rotation.z);
		
		
		//Front Right Wheel Transform.
		Vector3 ColliderCenterPointFR = Wheel_FR.transform.TransformPoint( Wheel_FR.center );
		Wheel_FR.GetGroundHit( out CorrespondingGroundHit );
		
		if ( Physics.Raycast( ColliderCenterPointFR, -Wheel_FR.transform.up, out hit, (Wheel_FR.suspensionDistance + Wheel_FR.radius) * transform.localScale.y ) ) {
			if(hit.transform.gameObject.layer != LayerMask.NameToLayer("CarCollider")){
				FrontRightWheelT.transform.position = hit.point + (Wheel_FR.transform.up * Wheel_FR.radius) * transform.localScale.y;
				float extension = (-Wheel_FR.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - Wheel_FR.radius) / Wheel_FR.suspensionDistance;
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + Wheel_FR.transform.up * (CorrespondingGroundHit.force / 8000), extension <= 0.0 ? Color.magenta : Color.white);
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_FR.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_FR.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
			}
		}else{
			FrontRightWheelT.transform.position = ColliderCenterPointFR - (Wheel_FR.transform.up * Wheel_FR.suspensionDistance) * transform.localScale.y;
		}
		
		if(fwd && Speed < 20 && motorInput > 0 && canBurnout)
			RotationValueFR += Wheel_FR.rpm * ( 12 ) * Time.deltaTime;
		else
			RotationValueFR += Wheel_FR.rpm * ( 6 ) * Time.deltaTime;
		
		FrontRightWheelT.transform.rotation = Wheel_FR.transform.rotation * Quaternion.Euler( RotationValueFR, Wheel_FR.steerAngle + (driftAngle / steeringAssistanceDivider), Wheel_FR.transform.rotation.z);
		
		
		//Rear Left Wheel Transform.
		Vector3 ColliderCenterPointRL = Wheel_RL.transform.TransformPoint( Wheel_RL.center );
		Wheel_RL.GetGroundHit( out CorrespondingGroundHit );
		
		if ( Physics.Raycast( ColliderCenterPointRL, -Wheel_RL.transform.up, out hit, (Wheel_RL.suspensionDistance + Wheel_RL.radius) * transform.localScale.y ) ) {
			if(hit.transform.gameObject.layer != LayerMask.NameToLayer("CarCollider")){
				RearLeftWheelT.transform.position = hit.point + (Wheel_RL.transform.up * Wheel_RL.radius) * transform.localScale.y;
				float extension = (-Wheel_RL.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - Wheel_RL.radius) / Wheel_RL.suspensionDistance;
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + Wheel_RL.transform.up * (CorrespondingGroundHit.force / 8000), extension <= 0.0 ? Color.magenta : Color.white);
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_RL.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_RL.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
			}
		}else{
			RearLeftWheelT.transform.position = ColliderCenterPointRL - (Wheel_RL.transform.up * Wheel_RL.suspensionDistance) * transform.localScale.y;
		}
		RearLeftWheelT.transform.rotation = Wheel_RL.transform.rotation * Quaternion.Euler( RotationValueRL, 0, Wheel_RL.transform.rotation.z);
		
		if(rwd && Speed < 20 && motorInput > 0 && canBurnout)
			RotationValueRL += Wheel_RL.rpm * ( 12 ) * Time.deltaTime;
		else
			RotationValueRL += Wheel_RL.rpm * ( 6 ) * Time.deltaTime;
		
		
		//Rear Right Wheel Transform.
		Vector3 ColliderCenterPointRR = Wheel_RR.transform.TransformPoint( Wheel_RR.center );
		Wheel_RR.GetGroundHit( out CorrespondingGroundHit );
		
		if ( Physics.Raycast( ColliderCenterPointRR, -Wheel_RR.transform.up, out hit, (Wheel_RR.suspensionDistance + Wheel_RR.radius) * transform.localScale.y ) ) {
			if(hit.transform.gameObject.layer != LayerMask.NameToLayer("CarCollider")){
				RearRightWheelT.transform.position = hit.point + (Wheel_RR.transform.up * Wheel_RR.radius) * transform.localScale.y;
				float extension = (-Wheel_RR.transform.InverseTransformPoint(CorrespondingGroundHit.point).y - Wheel_RR.radius) / Wheel_RR.suspensionDistance;
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point + Wheel_RR.transform.up * (CorrespondingGroundHit.force / 8000), extension <= 0.0 ? Color.magenta : Color.white);
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_RR.transform.forward * CorrespondingGroundHit.forwardSlip, Color.green);
				Debug.DrawLine(CorrespondingGroundHit.point, CorrespondingGroundHit.point - Wheel_RR.transform.right * CorrespondingGroundHit.sidewaysSlip, Color.red);
			}
		}else{
			RearRightWheelT.transform.position = ColliderCenterPointRR - (Wheel_RR.transform.up * Wheel_RR.suspensionDistance) * transform.localScale.y;
		}
		RearRightWheelT.transform.rotation = Wheel_RR.transform.rotation * Quaternion.Euler( RotationValueRR, 0, Wheel_RR.transform.rotation.z);
		
		if(rwd && Speed < 20 && motorInput > 0 && canBurnout)
			RotationValueRR += Wheel_RR.rpm * ( 12 ) * Time.deltaTime;
		else
			RotationValueRR += Wheel_RR.rpm * ( 6 ) * Time.deltaTime;

		if(ExtraRearWheels.Length > 0){
			
			for(int i = 0; i < ExtraRearWheels.Length; i++){
				
				Vector3 ColliderCenterPointExtra = ExtraRearWheels[i].transform.TransformPoint( ExtraRearWheels[i].center );
				
				if ( Physics.Raycast( ColliderCenterPointExtra, -ExtraRearWheels[i].transform.up, out hit, (ExtraRearWheels[i].suspensionDistance + ExtraRearWheels[i].radius) * transform.localScale.y ) ) {
					ExtraRearWheelsT[i].transform.position = hit.point + (ExtraRearWheels[i].transform.up * ExtraRearWheels[i].radius) * transform.localScale.y;
				}else{
					ExtraRearWheelsT[i].transform.position = ColliderCenterPointExtra - (ExtraRearWheels[i].transform.up * ExtraRearWheels[i].suspensionDistance) * transform.localScale.y;
					ExtraRearWheels[i].brakeTorque = Brake/10;
				}
				ExtraRearWheelsT[i].transform.rotation = ExtraRearWheels[i].transform.rotation * Quaternion.Euler( RotationValueExtra[i], 0, ExtraRearWheels[i].transform.rotation.z);
				RotationValueExtra[i] += ExtraRearWheels[i].rpm * ( 6 ) * Time.deltaTime;
				ExtraRearWheels[i].GetGroundHit( out CorrespondingGroundHit );
				
			}
			
		}
		
		//Drift Angle Calculation.
		WheelHit CorrespondingGroundHit5;
		Wheel_RR.GetGroundHit(out CorrespondingGroundHit5);
		driftAngle = Mathf.Lerp ( driftAngle, (Mathf.Clamp (CorrespondingGroundHit5.sidewaysSlip, -35, 35)), Time.deltaTime * 2 );
		
		//Driver SteeringWheel.
		if(SteeringWheel)
			SteeringWheel.transform.rotation = transform.rotation * Quaternion.Euler( 0, 0, (Wheel_FL.steerAngle + (driftAngle / steeringAssistanceDivider)) * -6);
		
	}
	
	void Braking(){
		
		//HandBrake
		if(Input.GetButton("Jump") || andHandBrake){
			
			Wheel_RL.brakeTorque = Brake;
			Wheel_RR.brakeTorque = Brake;
			Wheel_FL.brakeTorque = Brake/2;
			Wheel_FR.brakeTorque = Brake/2;
			RearLeftFriction.stiffness = handbrakeStiffness;
			RearRightFriction.stiffness = handbrakeStiffness;
			FrontLeftFriction.stiffness = handbrakeStiffness;
			FrontRightFriction.stiffness = handbrakeStiffness;
			Wheel_FL.sidewaysFriction = FrontRightFriction;
			Wheel_FR.sidewaysFriction = FrontLeftFriction;
			Wheel_RL.sidewaysFriction = RearLeftFriction;
			Wheel_RR.sidewaysFriction = RearRightFriction;
			
			//Normal Brake
		}else{
				
			if(motorInput == 0){
				Wheel_RL.brakeTorque = Brake/10f;
				Wheel_RR.brakeTorque = Brake/10f;
				Wheel_FL.brakeTorque = Brake/10f;
				Wheel_FR.brakeTorque = Brake/10f;
			}else if(motorInput < 0 && Wheel_FL.rpm > 0){
				Wheel_FL.brakeTorque = Brake * (Mathf.Abs(motorInput));
				Wheel_FR.brakeTorque = Brake * (Mathf.Abs(motorInput));
				Wheel_RL.brakeTorque = Brake * (Mathf.Abs(motorInput));
				Wheel_RR.brakeTorque = Brake * (Mathf.Abs(motorInput));
			}else{
				Wheel_RL.brakeTorque = 0;
				Wheel_RR.brakeTorque = 0;
				Wheel_FL.brakeTorque = 0;
				Wheel_FR.brakeTorque = 0;
			}
			
			if(!driftMode){
				RearLeftFriction.stiffness = Mathf.Lerp (RearLeftFriction.stiffness, StiffnessRear, Time.deltaTime*2);
				RearRightFriction.stiffness = Mathf.Lerp (RearRightFriction.stiffness, StiffnessRear, Time.deltaTime*2);
				FrontLeftFriction.stiffness = Mathf.Lerp (FrontLeftFriction.stiffness, StiffnessFront, Time.deltaTime*2);
				FrontRightFriction.stiffness = Mathf.Lerp (FrontRightFriction.stiffness, StiffnessFront, Time.deltaTime*2);
				Wheel_FL.sidewaysFriction = FrontRightFriction;
				Wheel_FR.sidewaysFriction = FrontLeftFriction;
				Wheel_RL.sidewaysFriction = RearLeftFriction;
				Wheel_RR.sidewaysFriction = RearRightFriction;
			}else{
				if(steerInput != 0){
					RearLeftFriction.stiffness = Mathf.Lerp (RearLeftFriction.stiffness, .1f, Time.deltaTime*2);
					RearRightFriction.stiffness = Mathf.Lerp (RearRightFriction.stiffness, .1f, Time.deltaTime*2);
					FrontLeftFriction.stiffness = Mathf.Lerp (FrontLeftFriction.stiffness, StiffnessFront, Time.deltaTime*2);
					FrontRightFriction.stiffness = Mathf.Lerp (FrontRightFriction.stiffness, StiffnessFront, Time.deltaTime*2);
					Wheel_FL.sidewaysFriction = FrontRightFriction;
					Wheel_FR.sidewaysFriction = FrontLeftFriction;
					Wheel_RL.sidewaysFriction = RearLeftFriction;
					Wheel_RR.sidewaysFriction = RearRightFriction;
				}else{
					RearLeftFriction.stiffness = Mathf.Lerp (RearLeftFriction.stiffness, StiffnessRear, Time.deltaTime);
					RearRightFriction.stiffness = Mathf.Lerp (RearRightFriction.stiffness, StiffnessRear, Time.deltaTime);
					FrontLeftFriction.stiffness = Mathf.Lerp (FrontLeftFriction.stiffness, StiffnessFront, Time.deltaTime);
					FrontRightFriction.stiffness = Mathf.Lerp (FrontRightFriction.stiffness, StiffnessFront, Time.deltaTime);
					Wheel_FL.sidewaysFriction = FrontRightFriction;
					Wheel_FR.sidewaysFriction = FrontLeftFriction;
					Wheel_RL.sidewaysFriction = RearLeftFriction;
					Wheel_RR.sidewaysFriction = RearRightFriction;
				}
			}
			
		}
		
	}
	
	void  OnGUI (){
		
		GUI.skin.font = dashBoardFont;
		GUI.skin.label.fontSize = 12;
		GUI.skin.box.fontSize = 12;
		Matrix4x4 orgRotation = GUI.matrix;
		
		if(canControl){
			
			if(useAccelerometerForSteer){
				leftArrow.gameObject.SetActive(false);
				rightArrow.gameObject.SetActive(false);
				handBrakeGui.gameObject.SetActive(true);
				brakePedal.transform.position = leftArrow.transform.position;
				steeringWheelControl = false;
			}else if(mobileController){
				leftArrow.gameObject.SetActive(true);
				rightArrow.gameObject.SetActive(true);
				handBrakeGui.gameObject.SetActive(true);
				brakePedal.transform.position = defBrakePedalPosition;
			}
			
			if(mobileController){
				gasPedal.gameObject.SetActive(true);
				brakePedal.gameObject.SetActive(true);
				handBrakeGui.gameObject.SetActive(true);
			}
			
			if(steeringWheelControl && mobileController){
				leftArrow.gameObject.SetActive(false);
				rightArrow.gameObject.SetActive(false);
				
				GUIUtility.RotateAroundPivot( -steeringWheelsteerAngle , steeringWheelTextureRect.center + steeringWheelPivotPos );
				GUI.DrawTexture( steeringWheelTextureRect, steeringWheelTexture );
				GUI.matrix = orgRotation;
			}
			
			if( demoGUI ) {
				
				GUI.backgroundColor = Color.black;
				
				GUI.Box(new Rect(Screen.width-410 - guiWidth, 10 + guiHeight, 400, 220), "");
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 10 + guiHeight, 400, 150), "Engine RPM : " + Mathf.CeilToInt(EngineRPM));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 30 + guiHeight, 400, 150), "Speed : " + Mathf.CeilToInt(Speed));
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 190 + guiHeight, 400, 150), "Horizontal Tilt : " + Input.acceleration.x);
				GUI.Label(new Rect(Screen.width-400 - guiWidth, 210 + guiHeight, 400, 150), "Vertical Tilt : " + Input.acceleration.y);
				if(fwd){
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 50 + guiHeight, 400, 150), "Left Wheel RPM : " + Mathf.CeilToInt(Wheel_FL.rpm));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 70 + guiHeight, 400, 150), "Right Wheel RPM : " + Mathf.CeilToInt(Wheel_FR.rpm));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 90 + guiHeight, 400, 150), "Left Wheel Torque : " + Mathf.CeilToInt(Wheel_FL.motorTorque));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 110 + guiHeight, 400, 150), "Right Wheel Torque : " + Mathf.CeilToInt(Wheel_FR.motorTorque));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 130 + guiHeight, 400, 150), "Left Wheel Brake : " + Mathf.CeilToInt(Wheel_FL.brakeTorque));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 150 + guiHeight, 400, 150), "Right Wheel Brake : " + Mathf.CeilToInt(Wheel_FR.brakeTorque));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 170 + guiHeight, 400, 150), "Steer Angle : " + Mathf.CeilToInt(Wheel_FL.steerAngle));
				}
				if(rwd){
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 50 + guiHeight, 400, 150), "Left Wheel RPM : " + Mathf.CeilToInt(Wheel_RL.rpm));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 70 + guiHeight, 400, 150), "Right Wheel RPM : " + Mathf.CeilToInt(Wheel_RR.rpm));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 90 + guiHeight, 400, 150), "Left Wheel Torque : " + Mathf.CeilToInt(Wheel_RL.motorTorque));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 110 + guiHeight, 400, 150), "Right Wheel Torque : " + Mathf.CeilToInt(Wheel_RR.motorTorque));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 130 + guiHeight, 400, 150), "Left Wheel Brake : " + Mathf.CeilToInt(Wheel_RL.brakeTorque));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 150 + guiHeight, 400, 150), "Right Wheel Brake : " + Mathf.CeilToInt(Wheel_RR.brakeTorque));
					GUI.Label(new Rect(Screen.width-400 - guiWidth, 170 + guiHeight, 400, 150), "Steer Angle : " + Mathf.CeilToInt(Wheel_FL.steerAngle));
				}
				
				GUI.backgroundColor = Color.blue;
				GUI.Button (new Rect(Screen.width-30 - guiWidth, 165 + guiHeight, 10, Mathf.Clamp((-motorInput * 100), -100, 0)), "");
				if(mobileController){
					if(GUI.Button(new Rect(Screen.width - 275, 200, 250, 50), "Use Accelerometer \n For Steer")){
						if(useAccelerometerForSteer)
							useAccelerometerForSteer = false;
						else useAccelerometerForSteer = true;
					}
					
					if(GUI.Button(new Rect(Screen.width - 275, 275, 250, 50), "Use Steering Wheel")){
						if(steeringWheelControl)
							steeringWheelControl = false;
						else steeringWheelControl = true;
					}
				}
				
				GUI.backgroundColor = Color.red;
				GUI.Button (new Rect(Screen.width-45 - guiWidth, 165 + guiHeight, 10, Mathf.Clamp((motorInput * 100), -100, 0)), "");
				
			}
			
			if( dashBoard ) {
				
				GUI.skin.label.fontSize = 18;
				
				needleRotation = Mathf.Lerp (0, 180, (EngineRPM - MinEngineRPM / 1.5f) / (MaxEngineRPM + MinEngineRPM));
				kMHneedleRotation = Mathf.Lerp (-18, 210, Speed/240);
				smoothedNeedleRotation = Mathf.Lerp (smoothedNeedleRotation, needleRotation, Time.deltaTime*2);
				
				if(Wheel_FL.rpm > -10)
					GUI.Label (new Rect(Screen.width-90, 150, 40, 40), "" + (CurrentGear + 1));
				else
					GUI.Label (new Rect(Screen.width-90, 150, 40, 40), "R" );
				
				GUI.DrawTexture(new Rect(Screen.width-270, 50, 256, 128), speedOMeter);
				GUI.Label (new Rect(Screen.width-225, 150, 100, 40), "" + Mathf.CeilToInt(EngineRPM));
				GUI.Label (new Rect(84, 116, 100, 40), "" + Mathf.CeilToInt(Speed));
				
				GUI.DrawTexture(new Rect(10, 10, 180, 180), kiloMeter);
				
				GUIUtility.RotateAroundPivot(smoothedNeedleRotation, new Vector2(Screen.width-142, 178));
				GUI.DrawTexture(new Rect(Screen.width-270, 50, 256, 256), speedOMeterNeedle);
				
				GUI.matrix = orgRotation;
				GUIUtility.RotateAroundPivot(kMHneedleRotation, new Vector2(100, 100));
				GUI.DrawTexture(new Rect(45, 40, 110, 130), kiloMeterNeedle);
				GUI.matrix = orgRotation;
			}
			
		}
		
	}
	
	void SmokeInstantiateRate () {
		
		WheelHit CorrespondingGroundHit0;
		WheelHit CorrespondingGroundHit1;
		WheelHit CorrespondingGroundHit2;
		WheelHit CorrespondingGroundHit3;
		
		if ( WheelParticles.Count > 0 ) {
			
			if(Speed > 40){
				
				Wheel_FR.GetGroundHit( out CorrespondingGroundHit0 );
				if(Mathf.Abs(CorrespondingGroundHit0.sidewaysSlip) > 15f || Mathf.Abs(CorrespondingGroundHit0.forwardSlip) > 7f ){
					WheelParticles[0].GetComponent<ParticleEmitter>().emit = true;
				}else{ 
					WheelParticles[0].GetComponent<ParticleEmitter>().emit = false;
				}
				
				Wheel_FL.GetGroundHit( out CorrespondingGroundHit1 );
				if(Mathf.Abs(CorrespondingGroundHit1.sidewaysSlip) > 15f || Mathf.Abs(CorrespondingGroundHit1.forwardSlip) > 7f ){
					WheelParticles[1].GetComponent<ParticleEmitter>().emit = true;
				}else{ 
					WheelParticles[1].GetComponent<ParticleEmitter>().emit = false;
				}
				
				Wheel_RR.GetGroundHit( out CorrespondingGroundHit2 );
				if(Mathf.Abs(CorrespondingGroundHit2.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit2.forwardSlip) > 6f ){
					WheelParticles[2].GetComponent<ParticleEmitter>().emit = true;
				}else{
					WheelParticles[2].GetComponent<ParticleEmitter>().emit = false;
				}
				
				Wheel_RL.GetGroundHit( out CorrespondingGroundHit3 );
				if(Mathf.Abs(CorrespondingGroundHit3.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit3.forwardSlip) > 6f ){
					WheelParticles[3].GetComponent<ParticleEmitter>().emit = true;
				}else{ 
					WheelParticles[3].GetComponent<ParticleEmitter>().emit = false;
				}
				
			}else if(canBurnout){
				
				Wheel_FR.GetGroundHit( out CorrespondingGroundHit0 );
				if(Mathf.Abs(CorrespondingGroundHit0.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit0.forwardSlip) > 7f )
					WheelParticles[0].GetComponent<ParticleEmitter>().emit = true;
				else WheelParticles[0].GetComponent<ParticleEmitter>().emit = false;
				
				Wheel_FL.GetGroundHit( out CorrespondingGroundHit1 );
				if(Mathf.Abs(CorrespondingGroundHit1.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit1.forwardSlip) > 7f ) 
					WheelParticles[1].GetComponent<ParticleEmitter>().emit = true;
				else WheelParticles[1].GetComponent<ParticleEmitter>().emit = false;
				
				Wheel_RR.GetGroundHit( out CorrespondingGroundHit2 );
				if(Mathf.Abs(CorrespondingGroundHit2.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit2.forwardSlip) > 6f ) 
					WheelParticles[2].GetComponent<ParticleEmitter>().emit = true;
				else WheelParticles[2].GetComponent<ParticleEmitter>().emit = false;
				
				Wheel_RL.GetGroundHit( out CorrespondingGroundHit3 );
				if(Mathf.Abs(CorrespondingGroundHit3.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit3.forwardSlip) > 6f )    
					WheelParticles[3].GetComponent<ParticleEmitter>().emit = true;
				else WheelParticles[3].GetComponent<ParticleEmitter>().emit = false;
				
			}else{
				
				Wheel_FR.GetGroundHit( out CorrespondingGroundHit0 );
				if(Mathf.Abs(CorrespondingGroundHit0.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit0.forwardSlip) > 7f ) 
					WheelParticles[0].GetComponent<ParticleEmitter>().emit = true;
				else WheelParticles[0].GetComponent<ParticleEmitter>().emit = false;
				
				Wheel_FL.GetGroundHit( out CorrespondingGroundHit1 );
				if(Mathf.Abs(CorrespondingGroundHit1.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit1.forwardSlip) > 7f ) 
					WheelParticles[1].GetComponent<ParticleEmitter>().emit = true;
				else WheelParticles[1].GetComponent<ParticleEmitter>().emit = false;
				
				Wheel_RR.GetGroundHit( out CorrespondingGroundHit2 );
				if(Mathf.Abs(CorrespondingGroundHit2.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit2.forwardSlip) > 6f ) 
					WheelParticles[2].GetComponent<ParticleEmitter>().emit = true;
				else WheelParticles[2].GetComponent<ParticleEmitter>().emit = false;
				
				Wheel_RL.GetGroundHit( out CorrespondingGroundHit3 );
				if(Mathf.Abs(CorrespondingGroundHit3.sidewaysSlip) > 5f || Mathf.Abs(CorrespondingGroundHit3.forwardSlip) > 6f )   
					WheelParticles[3].GetComponent<ParticleEmitter>().emit = true;
				else WheelParticles[3].GetComponent<ParticleEmitter>().emit = false;
				
			}
			
		}
		
		if(normalExhaustGas && canControl){
			if(Speed < 20)
				normalExhaustGas.emit = true;
			else normalExhaustGas.emit = false;
		}
		
		if(heavyExhaustGas && canControl){
			if(Speed < 10 && motorInput > 0)
				heavyExhaustGas.emit = true;
			else heavyExhaustGas.emit = false;
		}
		
		if(!canControl){
			heavyExhaustGas.emit = false;
			normalExhaustGas.emit = false;
		}
		
	}
	
	void SkidAudio(){
		
		WheelHit CorrespondingGroundHit;
		Wheel_FR.GetGroundHit( out CorrespondingGroundHit );
		
		if(Mathf.Abs(CorrespondingGroundHit.sidewaysSlip) > 5 || Mathf.Abs(CorrespondingGroundHit.forwardSlip) > 7) 
			skidAudio.GetComponent<AudioSource>().volume = Mathf.Abs(CorrespondingGroundHit.sidewaysSlip)/10 + Mathf.Abs(CorrespondingGroundHit.forwardSlip)/10;
		else
			skidAudio.GetComponent<AudioSource>().volume -= Time.deltaTime;
		
	}
	
	void OnCollisionEnter( Collision collision ){
		
		
		if (collision.contacts.Length > 0){	
			
			if(collision.relativeVelocity.magnitude > collisionForceLimit && crashClips.Length > 0){
				if (collision.contacts[0].thisCollider.gameObject.layer != LayerMask.NameToLayer("Wheel") && collision.gameObject.layer != LayerMask.NameToLayer("Road")){
					crashAudio.GetComponent<AudioSource>().clip = crashClips[Random.Range(0, crashClips.Length)];
					crashAudio.GetComponent<AudioSource>().pitch = Random.Range (1f, 1.2f);
					crashAudio.GetComponent<AudioSource>().Play ();
				}
			}
			
		}
		
	}
	
	void Chassis(){
		
		WheelHit CorrespondingGroundHit;
		Wheel_RR.GetGroundHit( out CorrespondingGroundHit );
		
		verticalLean = Mathf.Clamp(Mathf.Lerp (verticalLean, GetComponent<Rigidbody>().angularVelocity.x * chassisVerticalLean, Time.deltaTime * 5), -3.0f, 3.0f);
		horizontalLean = Mathf.Clamp(Mathf.Lerp (horizontalLean, GetComponent<Rigidbody>().angularVelocity.y * chassisHorizontalLean, Time.deltaTime * 3), -5.0f, 5.0f);
		
		Quaternion target = Quaternion.Euler(verticalLean, chassis.transform.localRotation.y, horizontalLean);
		chassis.transform.localRotation = target;
		
	}
	
	void Lights(){
		
		float lightInput;
		lightInput = Mathf.Clamp(-motorInput * 2, 0.0f, 1.0f);
		
		if(Input.GetKeyDown(KeyCode.L)){
			if(headLightsOn)
				headLightsOn = false;
			else headLightsOn = true;
		}
		
		for(int i = 0; i < BrakeLights.Length; i++){
			
			if(Wheel_FL.rpm > 0)
				BrakeLights[i].intensity = lightInput;
			else
				BrakeLights[i].intensity = Mathf.Lerp (BrakeLights[i].intensity, 0.0f, Time.deltaTime*5);
			
		}
		
		for(int i = 0; i < HeadLights.Length; i++){
			
			if(headLightsOn)
				HeadLights[i].enabled = true;
			else
				HeadLights[i].enabled = false;
			
		}
		
		for(int i = 0; i < ReverseLights.Length; i++){
			
			if(Wheel_FL.rpm > 0)
				ReverseLights[i].intensity = Mathf.Lerp (ReverseLights[i].intensity, 0.0f, Time.deltaTime*5);
			else
				ReverseLights[i].intensity = lightInput;
			
		}
		
	}
	
}