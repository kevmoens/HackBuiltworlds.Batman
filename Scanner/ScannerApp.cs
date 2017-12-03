using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared;
using Urho;
using Urho.Actions;
using Urho.Gui;
using Urho.HoloLens;
using Urho.Shapes;

namespace SmartHome.HoloLens
{
	public class ScannerApp : HoloApplication
	{
        Node menuNode;
		Node environmentNode;
		SpatialCursor cursor;
		Material material;
		ClientConnection clientConnection;
        Guid SessionID;
        bool IsPrimary; //Is Primary Session 
        string _surfaceID;
        bool _Debug;
        System.Threading.Timer timer;
        System.Collections.Concurrent.ConcurrentDictionary<string, BulbAddedDto> ExistingBulbs;

        public ScannerApp(ApplicationOptions opts) : base(opts) { }

		protected override async void Start()
		{
			base.Start();
			clientConnection = new ClientConnection();
			clientConnection.Disconnected += ClientConnection_Disconnected;
			clientConnection.RegisterForRealtimeUpdate(GetCurrentPositionDto);

            ExistingBulbs = new System.Collections.Concurrent.ConcurrentDictionary<string, BulbAddedDto>();

            Zone.AmbientColor = new Color(0.3f, 0.3f, 0.3f);
			DirectionalLight.Brightness = 0.5f;

			environmentNode = Scene.CreateChild();
			EnableGestureTapped = true;

			//material = Material.FromColor(Color.Gray); //-- debug mode
            _Debug = false;
            material = Material.FromColor(Color.Transparent, true);
            material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitVCol, 1, 1);
            
            Action placeOutlet = delegate ()
            {
                var outletBase = Scene.CreateChild("OUTLET");
                outletBase.Scale = new Vector3(1, 1f, 1) / 10;
                outletBase.Position = cursor.CursorNode.WorldPosition;
                outletBase.SetDirection(cursor.CursorNode.WorldDirection);


                var nodeOutlet = outletBase.CreateChild();
            //nodeOutlet.Rotate(new Quaternion(0, 90, 90, 90), TransformSpace.Local);


            nodeOutlet.Rotation = new Quaternion( 0, 270, -90); // cursor.CursorNode.Rotation.ToEulerAngles().Y, 0);
                //nodeOutlet.RotateAround(new Vector3(0, 0, 0), new Quaternion(0, 270, 90), TransformSpace.Local); //KMM


                nodeOutlet.Position = new Vector3(0, 0, -.25f);
                nodeOutlet.SetScale(.5f);
                //var outletBox = nodeOutlet.CreateComponent<Box>();
                //var material = Material.FromColor(Color.Red, true);
                //outletBox.SetMaterial(material);


                var modelOutlet = nodeOutlet.CreateComponent<StaticModel>();
                modelOutlet.Model = ResourceCache.GetModel("Data\\outlet.mdl");
            };
            
            Action OpenMenu = delegate ()
            {


                if (menuNode == null)
                {
                    //var menuBase = LeftCamera.Node.CreateChild("MENU");  //Following Menu
                    //menuBase.Position = new Vector3(0, -.2f, 0);

                    var menuBase = Scene.CreateChild("MENU");
                    menuBase.Position = new Vector3(0, 0 , 0);
                    menuBase.Scale = new Vector3(1, 1f, 1) / 10;
                    menuBase.Position = LeftCamera.Node.WorldPosition; //Comment Out for Following Menu
                    menuBase.SetDirection(LeftCamera.Node.Direction); //Comment Out for Following Menu




                    var menu = menuBase.CreateChild("MENUBOX");
                    menu.Position = new Vector3(0, 0, 10f);
                    var vectorDistance = menuBase.Position - LeftCamera.Node.Position;
                    Color color = Color.White;
                    color.A = .1f;
                    var matrl = Material.FromColor(Color.White, true);
                    menu.Scale = new Vector3(3f, 3f, 0.25f);
                    var box = menu.CreateComponent<Box>();
                    box.SetMaterial(matrl);



                    var menuCaptionBox = menuBase.CreateChild("MENUBOX");
                    menuCaptionBox.Position = new Vector3(0, 2, 10f);
                    color.A = .1f;
                    var matrlGray = Material.FromColor(Color.Gray, true);
                    menuCaptionBox.Scale = new Vector3(3f, 1f, 0.25f);
                    var Captionbox = menuCaptionBox.CreateComponent<Box>();
                    Captionbox.SetMaterial(matrlGray);


                    var textNode = menuBase.CreateChild();
                    textNode.Position = new Vector3(0, 2, 9.7f);
                    //textNode.SetScale(0.1f);
                    var text = textNode.CreateComponent<Text3D>();
                    text.Text = "Menu";
                    text.SetFont(CoreAssets.Fonts.AnonymousPro, 20);
                    text.HorizontalAlignment = HorizontalAlignment.Center;
                    text.VerticalAlignment = VerticalAlignment.Center;
                    text.TextAlignment = HorizontalAlignment.Center;
                    text.SetColor(Color.Green);


                    var nodeOutlet = menuBase.CreateChild();
                    nodeOutlet.Position = new Vector3(-1, 1, 9.7f);
                    nodeOutlet.SetScale(.25f);
                    var modelOutlet = nodeOutlet.CreateComponent<StaticModel>();
                    modelOutlet.Model = ResourceCache.GetModel("Data\\outlet.mdl");


                    var outletTextNode = menuBase.CreateChild();
                    outletTextNode.Position = new Vector3(.5f, 1, 9.7f);
                    var outletText = outletTextNode.CreateComponent<Text3D>();
                    outletText.Text = "Outlet";
                    outletText.SetFont(CoreAssets.Fonts.AnonymousPro, 12);
                    outletText.HorizontalAlignment = HorizontalAlignment.Center;
                    outletText.VerticalAlignment = VerticalAlignment.Center;
                    outletText.TextAlignment = HorizontalAlignment.Center;
                    outletText.SetColor(Color.Green);


                    menuNode = menuBase;
                }
            };

            Action CloseMenu = delegate ()
            {
                if (menuNode != null)
                {
                    try
                    {
                        menuNode.Remove();
                        menuNode.Dispose();
                        menuNode = null;
                    } catch { }
                }
            };
            await RegisterCortanaCommands(new Dictionary<string, Action>() { 
                { "open menu", OpenMenu}, { "place", placeOutlet}, {"close menu", CloseMenu }
            });

            while (!await ConnectAsync()) { }

            timer = new System.Threading.Timer(new System.Threading.TimerCallback(CheckStatus), null, 5000, 5000);

        }

        public async Task<bool> ConnectAsync()
        {
            cursor?.Remove();
            cursor = null;

            var textNode = LeftCamera.Node.CreateChild();
            textNode.Position = new Vector3(0, 0, 1);
            textNode.SetScale(0.1f);
            var text = textNode.CreateComponent<Text3D>();
            text.Text = "Look at the QR code\nopened in Client app...";
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.TextAlignment = HorizontalAlignment.Center;
            text.SetFont(CoreAssets.Fonts.AnonymousPro, 20);
            text.SetColor(Color.Green);

            string ipAddressString = "", ip = "";
            int port;
            while (!Utils.TryParseIpAddress(ipAddressString, out ip, out port))
            {
#if VIDEO_RECORDING //see OnGestureDoubleTapped for comments
				ipAddressString = await fakeQrCodeResultTaskSource.Task; 
#else
                ipAddressString = await QrCodeReader.ReadAsync();
#endif
            }

            InvokeOnMain(() => text.Text = "Connecting...");


            Shared.SmartHomeService.SmartHomeService proxy = new Shared.SmartHomeService.SmartHomeService();
            //proxy.SetURI("http://" + ip + "/SmartHomeService/SmartHomeService.svc/rest");
            proxy.SetURI("http://" + ip + ":50424/SmartHomeService/SmartHomeService.svc/rest");
            SessionID = Guid.NewGuid();
            Shared.SmartHomeService.OpenSessionResult session = await proxy.OpenSession(SessionID.ToString());
            IsPrimary = session.IsPrimary;
            if (await clientConnection.ConnectAsync(ip, port))
            {
                clientConnection.SendObject(new NewSessionDto() { SessionID = SessionID.ToString()});

                InvokeOnMain(() => text.Text = "Connected!");
				await environmentNode.RunActionsAsync(new DelayTime(2));
				await StartSpatialMapping(new Vector3(100, 100, 100));
				InvokeOnMain(() =>
					{
						textNode.Remove();
						cursor = Scene.CreateComponent<SpatialCursor>();
					});
				return true;
			}
			return false;
		}

		async void ClientConnection_Disconnected()
		{
			StopSpatialMapping();
			await TextToSpeech("Disconnected");
			while (!await ConnectAsync()) { }
		}

		protected override void OnUpdate(float timeStep)
		{
			base.OnUpdate(timeStep);
			DirectionalLight.Node.SetDirection(LeftCamera.Node.Direction);
		}

		public async override void OnGestureTapped()
		{
			if (!clientConnection.Connected || cursor == null)
				return;


            var noteNode = Raycast();
            if (noteNode != null)
            {
                if (ExistingBulbs.ContainsKey(noteNode.Name))
                {
                    foreach (var childNode in noteNode.Children)
                    {
                        if (childNode.Name == "Text")
                        {
                            childNode.Enabled = !childNode.Enabled;
                            return;
                        }
                    }
                }
            }
      

            var speechRecognizer = new Windows.Media.SpeechRecognition.SpeechRecognizer();
            // Compile the dictation grammar by default.
            await speechRecognizer.CompileConstraintsAsync();
            string speechText = "";
            // Start recognition.
            var pos = cursor.CursorNode.WorldPosition;
            var dir = LeftCamera.Node.WorldDirection;
            try
            {
                Windows.Media.SpeechRecognition.SpeechRecognitionResult speechRecognitionResult = await speechRecognizer.RecognizeAsync();
                speechText = speechRecognitionResult.Text;
            }
            catch {
                return;
            }
            var child = Scene.CreateChild(Guid.NewGuid().ToString());
			child.Scale = new Vector3(1, 1f, 1) / 10;
           
			child.Position = pos;
            child.LookAt(dir , Vector3.UnitY, TransformSpace.Local);
            child.Position = new Vector3(pos.X , pos.Y , pos.Z - .05f );
            child.Rotate(new Quaternion(315f, 270f, 0f), TransformSpace.Local);

            var model = child.CreateComponent<StaticModel>();
            model.Model = ResourceCache.GetModel("Data\\thumbtack.mdl");
            var textNode = child.CreateChild("Text");
            textNode.Rotate(new Quaternion(-315f, -270f, 0f), TransformSpace.Local);
            var text = textNode.CreateComponent<Text3D>();
            text.Text = speechText;
            text.HorizontalAlignment = HorizontalAlignment.Center;
            text.VerticalAlignment = VerticalAlignment.Center;
            text.TextAlignment = HorizontalAlignment.Center;
            text.SetFont(CoreAssets.Fonts.AnonymousPro, 20);
            text.SetColor(Color.Green);
            //text.Opacity = 0f;

            BulbAddedDto bulb = new BulbAddedDto { ID = child.Name, Position = new Vector3Dto(pos.X, pos.Y, pos.Z), Text = speechText, Direction = new Vector3Dto(LeftCamera.Node.WorldDirection.X, LeftCamera.Node.WorldDirection.Y, LeftCamera.Node.WorldDirection.Z) };
            clientConnection.SendObject(bulb);

            ExistingBulbs.TryAdd(bulb.ID, bulb);
            Shared.SmartHomeService.SmartHomeService proxy = new Shared.SmartHomeService.SmartHomeService();
            await proxy.AddNote(bulb);
        }

		public override unsafe void OnSurfaceAddedOrUpdated(SpatialMeshInfo surface, Model generatedModel)
		{
			bool isNew = false;
			StaticModel staticModel = null;
            _surfaceID = surface.SurfaceId;
            Node node = environmentNode.GetChild(surface.SurfaceId, false);
			if (node != null)
			{
				isNew = false;
				staticModel = node.GetComponent<StaticModel>();
			}
			else
			{
				isNew = true;
				node = environmentNode.CreateChild(surface.SurfaceId);
				staticModel = node.CreateComponent<StaticModel>();
			}

			node.Position = surface.BoundsCenter;
			node.Rotation = surface.BoundsRotation;
			staticModel.Model = generatedModel;
			
			if (isNew)
			{
				staticModel.SetMaterial(material);
			}

			var surfaceDto = new SurfaceDto
			{
				Id = surface.SurfaceId,
				IndexData = surface.IndexData,
				BoundsCenter = new Vector3Dto(surface.BoundsCenter.X, surface.BoundsCenter.Y, surface.BoundsCenter.Z),
				BoundsOrientation = new Vector4Dto(surface.BoundsRotation.X, 
					surface.BoundsRotation.Y, surface.BoundsRotation.Z, surface.BoundsRotation.W),
				BoundsExtents = new Vector3Dto(surface.Extents.X, surface.Extents.Y, surface.Extents.Z)
			};

			var vertexData = surface.VertexData;
			surfaceDto.VertexData = new SpatialVertexDto[vertexData.Length];
			for (int i = 0; i < vertexData.Length; i++)
			{
				SpatialVertex vertexItem = vertexData[i];
				surfaceDto.VertexData[i] = *(SpatialVertexDto*)(void*)&vertexItem;
			}

            if (IsPrimary)
            {
                //Shared.SmartHomeService.SmartHomeService proxy = new Shared.SmartHomeService.SmartHomeService();
                //proxy.Ping().GetAwaiter().GetResult();

                clientConnection.SendObject(surfaceDto.Id, surfaceDto);
            }
		}

		BaseDto GetCurrentPositionDto()
		{
			var position = LeftCamera.Node.Position;
			var direction = LeftCamera.Node.Direction;
            return new CurrentPositionDto
            {
                SessionID = SessionID.ToString(),
				Position = new Vector3Dto(position.X, position.Y, position.Z),
				Direction = new Vector3Dto(direction.X, direction.Y, direction.Z)
			};
		}


        async void CheckStatus(Object state)
        {
            Shared.SmartHomeService.SmartHomeService proxy = new Shared.SmartHomeService.SmartHomeService();
            List<BulbAddedDto> bulbs = await proxy.GetNotes();
            lock (ExistingBulbs)
            {
                if (bulbs != null)
                {
                    foreach (BulbAddedDto bulb in bulbs)
                    {
                        //bool foundExisting = false;
                        //foreach (BulbAddedDto b in ExistingBulbs)
                        //{
                        //    if (bulb.Position.X == b.Position.X && bulb.Position.Y == b.Position.Y && bulb.Position.Z == b.Position.Z && bulb.Text == b.Text)
                        //    {
                        //        foundExisting = true;
                        //        break;
                        //    }
                        //}
                        //if (!foundExisting)
                        if (!ExistingBulbs.ContainsKey(bulb.ID))
                        {

                            var child = Scene.CreateChild();
                            child.Scale = new Vector3(1, 1f, 1) / 10;

                            child.Position = new Vector3(bulb.Position.X, bulb.Position.Y, bulb.Position.Z);
                            child.LookAt(new Vector3(bulb.Direction.X, bulb.Direction.Y, bulb.Direction.Z), Vector3.UnitY, TransformSpace.Local);
                            //child.Position = new Vector3(bulb.Position.X, bulb.Position.Y, bulb.Position.Z - .2f);
                            //child.Rotate(new Quaternion(0f, 0f, 0f), TransformSpace.Local);
                            var text = child.CreateComponent<Text3D>();
                            text.Text = bulb.Text;
                            text.HorizontalAlignment = HorizontalAlignment.Center;
                            text.VerticalAlignment = VerticalAlignment.Center;
                            text.TextAlignment = HorizontalAlignment.Center;
                            text.SetFont(CoreAssets.Fonts.AnonymousPro, 20);
                            text.SetColor(Color.Green);
                            ExistingBulbs.TryAdd(bulb.ID, bulb);

                        }
                    }
                }
            }
        }


        Node Raycast()
        {
            Ray cameraRay = LeftCamera.GetScreenRay(0.5f, 0.5f);
            var result = Scene.GetComponent<Octree>().RaycastSingle(cameraRay, RayQueryLevel.Triangle, 100, DrawableFlags.Geometry, 0x70000000);
            if (result != null)
            {
                return result.Value.Node;
            }
            return null;
        }
#if VIDEO_RECORDING
		TaskCompletionSource<string> fakeQrCodeResultTaskSource = new TaskCompletionSource<string>();
		public override void OnGestureDoubleTapped()
		{
			// Unfortunately, it's not allowed to record a video ("Hey Cortana, start recording")
			// and grab frames (in order to read a QR) at the same time - it will crash.
			// so I use a fake QR code result for the demo purposes
			// it is emulated by a double tap gesture
			Task.Run(() => fakeQrCodeResultTaskSource.TrySetResult("192.168.1.6:5206"));
		}
#endif
    }
}