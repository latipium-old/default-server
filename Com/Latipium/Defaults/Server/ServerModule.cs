// ServerModule.cs
//
// Copyright (c) 2016 Zach Deibert.
// All Rights Reserved.
using System;
using System.Threading;
using log4net;
using Com.Latipium.Core;

namespace Com.Latipium.Defaults.Server {
	/// <summary>
	/// The default module implementation for the server.
	/// </summary>
	public class ServerModule : AbstractLatipiumModule {
		private static ILog Log = LogManager.GetLogger(typeof(ServerModule));

		/// <summary>
		/// Starts the client.
		/// </summary>
		[LatipiumMethod("Start")]
		public void Start() {
			LatipiumModule network = ModuleFactory.FindModule("Com.Latipium.Modules.Network");
			LatipiumModule physics = ModuleFactory.FindModule("Com.Latipium.Modules.Physics");
			LatipiumModule player = ModuleFactory.FindModule("Com.Latipium.Modules.Player");
			LatipiumModule worldGen = ModuleFactory.FindModule("Com.Latipium.Modules.World.Generator");
			LatipiumModule worldSer = ModuleFactory.FindModule("Com.Latipium.Modules.World.Serialization");
			if ( network == null ) {
				Log.Error("Unable to find network module");
			} else {
				LatipiumObject world = null;
				if ( worldSer != null ) {
					world = worldSer.InvokeFunction<LatipiumObject>("Load");
				}
				if ( world == null && worldGen != null ) {
					world = worldGen.InvokeFunction<LatipiumObject>("Generate");
				}
				Thread physicsThread = null;
				Thread parent = null;
				if ( physics != null ) {
					physicsThread = new Thread(() => {
						physics.InvokeProcedure("Initialize");
						if ( world != null ) {
							physics.InvokeProcedure<LatipiumObject>("LoadWorld", world);
						}
						try {
							physics.InvokeProcedure("Loop");
						} catch ( ThreadInterruptedException ) {
						} finally {
							physics.InvokeProcedure("Destroy");
							if ( parent != null ) {
								parent.Interrupt();
							}
						}
					});
					physicsThread.Start();
				}
				network.InvokeProcedure("InitializeServer");
				if ( world != null ) {
					network.InvokeProcedure<LatipiumObject>("LoadWorld", world);
				}
				try {
					network.InvokeProcedure("Loop");
				} finally {
					network.InvokeProcedure("Destroy");
					if ( physicsThread != null ) {
						physicsThread.Interrupt();
						try {
							Thread.Sleep(int.MaxValue);
						} catch ( ThreadInterruptedException ) {
						}
					}
					if ( world != null && worldSer != null ) {
						worldSer.InvokeProcedure<LatipiumObject>("Save", world);
					}
				}
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Com.Latipium.Defaults.Server.ServerModule"/> class.
		/// </summary>
		public ServerModule() : base(new string[] { "Com.Latipium.Modules.Server" }) {
		}
	}
}

