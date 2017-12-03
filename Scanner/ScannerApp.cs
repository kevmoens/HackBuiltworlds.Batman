using Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Urho;
using Urho.Actions;
using Urho.Gui;
using Urho.HoloLens;
using Urho.Shapes;

namespace SmartHome.HoloLens
{
    public class ScannerApp : HoloApplication
	{
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

            await RegisterCortanaCommands(new Dictionary<string, Action> {
				{ "stop spatial mapping", StopSpatialMapping}
			});
            //Action ToggleMaterial = delegate ()
            //{
                

            //    if (_Debug)
            //    {
            //        material = Material.FromColor(Color.Transparent, true);
            //        _Debug = false;
            //    }
            //    else
            //    {
            //        material = Material.FromColor(Color.Gray);
            //        _Debug = true;
            //    }
                
            //    material.SetTechnique(0, CoreAssets.Techniques.NoTextureUnlitVCol, 1, 1);
            //    StaticModel staticModel = null;
            //    foreach ( Node node in environmentNode.Children)
            //    {

            //        staticModel = node.GetComponent<StaticModel>(true); // CreateComponent<StaticModel>();

            //        staticModel.SetMaterial(material);
            //    }
            //};

            //await RegisterCortanaCommands(new Dictionary<string, Action> {
            //    { "toggle mapping",  ToggleMaterial}
            //    });

            Action TestWebService = async delegate ()
            {
                Shared.SmartHomeService.SmartHomeService proxy = new Shared.SmartHomeService.SmartHomeService();
               
                await TextToSpeech(await proxy.GetData("TEST"));
            };

            await RegisterCortanaCommands(new Dictionary<string, Action> {
                { "test web service",  TestWebService}
                });



            Action PlaceOutlet = delegate ()
            {

                BuildObjectToMap("outlet", new Vector3(1f, 1f, 1f), 10);

                //var pos = cursor.CursorNode.WorldPosition;
                //var dir = LeftCamera.Node.WorldDirection;

                //var outlet = Scene.CreateChild(Guid.NewGuid().ToString());
                //outlet.Scale = new Vector3(1, 1f, 1) / 10;
                //outlet.Position = cursor.CursorNode.WorldPosition;
                ////outlet.Rotate(new Quaternion(0f, -45f, 0f), TransformSpace.Local);
                //outlet.SetDirection(cursor.CursorNode.Direction); // 

                //outlet.Position = pos;
                //outlet.LookAt(dir, Vector3.UnitY, TransformSpace.Local);
                ////outlet.Position = new Vector3(pos.X, pos.Y, pos.Z - .05f);
                ////outlet.Rotate(new Quaternion(315f, 270f, 0f), TransformSpace.Local);

                //var model = outlet.CreateComponent<Box>();
                //model.Model = ResourceCache.GetModel("Data\\outlet.mdl");
                //var textNode = outlet.CreateChild("Text");
                //textNode.Rotate(new Quaternion(-315f, -270f, 0f), TransformSpace.Local);
                //var text = textNode.CreateComponent<Text3D>();

            };
            Action PlaceFluxCapacitor = async delegate ()
            {
                BuildObjectToMap("fuxcap", new Vector3(1, 1f, 1), 1);
            };
             Action PlaceWaterHeater = async delegate ()
            {
                //var pos = cursor.CursorNode.WorldPosition;
                //var dir = LeftCamera.Node.WorldDirection;
                //var flux = Scene.CreateChild(Guid.NewGuid().ToString());
                //flux.Scale = new Vector3(1, 1f, 1) / 10;

                //flux.Position = pos;
                //flux.LookAt(dir, Vector3.UnitY, TransformSpace.Local);
                //flux.Position = new Vector3(pos.X, pos.Y, pos.Z - .05f);
                //flux.Rotate(new Quaternion(315f, 270f, 0f), TransformSpace.Local);

                //var model = flux.CreateComponent<StaticModel>();
                //model.Model = ResourceCache.GetModel("Data\\flux.mdl");
                //var textNode = flux.CreateChild("Text");
                //textNode.Rotate(new Quaternion(-315f, -270f, 0f), TransformSpace.Local);

                //var dir = LeftCamera.Node.WorldDirection;

                //var flux = Scene.CreateChild(Guid.NewGuid().ToString());
                //flux.Scale = new Vector3(1, 1f, 1) / 100;
                //flux.Position = cursor.CursorNode.WorldPosition;
                ////outlet.Rotate(new Quaternion(0f, -45f, 0f), TransformSpace.Local);
                //flux.SetDirection(cursor.CursorNode.Direction); // 

                //flux.LookAt(dir, Vector3.UnitY, TransformSpace.Local);
                ////outlet.Position = new Vector3(pos.X, pos.Y, pos.Z - .05f);
                ////outlet.Rotate(new Quaternion(315f, 270f, 0f), TransformSpace.Local);

                //var model = flux.CreateComponent<Box>();
                //model.Model = ResourceCache.GetModel("Data\\drum.mdl");
                //var textNode = flux.CreateChild("Text");

                BuildObjectToMap("drum", new Vector3(1, 1f, 1), 500);
               // textNode.Rotate(new Quaternion(-315f, -270f, 0f), TransformSpace.Local);

            };
            Action OpenMenu = delegate ()
            {
                
                var menu = Scene.CreateChild("MENU");
                menu.Scale = new Vector3(1, 1f, 1) / 10;
                menu.Position = cursor.CursorNode.WorldPosition;
                menu.SetDirection(LeftCamera.Node.Direction); // 
            
                //menu.LookAt(LeftCamera.Node.WorldDirection, Vector3.UnitY, TransformSpace.Local);
                //menu.Position = new Vector3(pos.X, pos.Y, pos.Z - .05f);

                System.Diagnostics.Debug.WriteLine("CAMERA POSITION:" + LeftCamera.Node.Position.ToString());
                System.Diagnostics.Debug.WriteLine("MENU POSITION:" + menu.Position.ToString());
                System.Diagnostics.Debug.WriteLine("DIRECTION:" + LeftCamera.Node.WorldDirection.ToString());

                //menu.Position = new Vector3(menu.Position.X, menu.Position.Y, menu.Position.Z - 1f);


                var matrl = Material.FromColor(Color.Red, true);

                
                var box = menu.CreateComponent<Box>();
                box.SetMaterial(matrl);
                //menu.Rotate(new Quaternion(315f, 270f, 0f), TransformSpace.Local);

            };
            await RegisterCortanaCommands(new Dictionary<string, Action> {
                { "open menu", OpenMenu },
                { "hello", PlaceWaterHeater },
                { "bye", PlaceOutlet}
                //{ "capacitor", PlaceFluxCapacitor }
            });
            //await RegisterCortanaCommands(new Dictionary<string, Action> {
            //    { "place outlet", PlaceObject}
            //});

            while (!await ConnectAsync()) { }

            timer = new System.Threading.Timer(new System.Threading.TimerCallback(CheckStatus), null, 5000, 5000);

        }
        public async void BuildObjectToMap(string objName, Vector3 vect, int scale_factor)
        {
            var pos = cursor.CursorNode.WorldPosition;
            var dir = LeftCamera.Node.WorldDirection;
            var object_to_map = Scene.CreateChild(Guid.NewGuid().ToString());
            object_to_map.Scale = vect / scale_factor;
            object_to_map.Position = cursor.CursorNode.WorldPosition;
            //outlet.Rotate(new Quaternion(0f, -45f, 0f), TransformSpace.Local);
            object_to_map.SetDirection(cursor.CursorNode.Direction); // 

            object_to_map.LookAt(dir, Vector3.UnitY, TransformSpace.Local);
            //outlet.Position = new Vector3(pos.X, pos.Y, posf.Z - .05f);
            //outlet.Rotate(new Quaternion(315f, 270f, 0f), TransformSpace.Local);

            var model = object_to_map.CreateComponent<Box>();
            model.Model = ResourceCache.GetModel("Data\\" + objName + ".mdl");

            var textNode = object_to_map.CreateChild("Text");
            var text = textNode.CreateComponent<Text3D>();
            BulbAddedDto bulb = new BulbAddedDto {scale_factor = scale_factor, obj_name = objName, Text = objName, ID = object_to_map.Name, Position = new Vector3Dto(pos.X, pos.Y, pos.Z), Direction = new Vector3Dto(LeftCamera.Node.WorldDirection.X, LeftCamera.Node.WorldDirection.Y, LeftCamera.Node.WorldDirection.Z) };
            clientConnection.SendObject(bulb);

            ExistingBulbs.TryAdd(bulb.ID, bulb);
            Shared.SmartHomeService.SmartHomeService proxy = new Shared.SmartHomeService.SmartHomeService();
            await proxy.AddNote(bulb);

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
            proxy.SetURI("http://" + ip + "/SmartHomeService/SmartHomeService.svc/rest");
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