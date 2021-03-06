/***************************************************************************
 *                                 Config.cs
 *                            -------------------
 *   begin                : Jan 30, 2005
 *   copyright            : (C) 2005 Max Kellermann <max@duempel.org>
 *   email                : max@duempel.org
 *
 *   $Id$
 *   $Author$
 *   $Date$
 *
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Net;

namespace Server {
	public class LibraryConfig {
		private string name;
		private DirectoryInfo sourcePath;
		private FileInfo binaryPath;
		private bool disabled = false;
		private string[] ignoreSources;
		private string[] ignoreTypes;
		private string[] depends;
		private string[] overlays;
		private int warningLevel = -1;

		private static string[] CollectStringArray(XmlElement parent,
												   string tag, string attr) {
			ArrayList al = new ArrayList();
			foreach (XmlElement el in parent.GetElementsByTagName(tag)) {
				string n = el.GetAttribute(attr);
				if (n != null && n != "")
					al.Add(n);
			}

			return al.Count == 0
				? null
				: (string[])al.ToArray(typeof(string));
		}
		private static string[] LowerStringArray(string[] src) {
			if (src == null)
				return null;

			string[] dst = new string[src.Length];
			for (uint i = 0; i < src.Length; i++)
				dst[i] = src[i].ToLower();

			return dst;
		}

		private static string GetElementString(XmlElement parent, string tag) {
			XmlNodeList nl = parent.GetElementsByTagName(tag);
			if (nl.Count == 0)
				return null;
			return nl[0].InnerText;
		}

		public LibraryConfig(string _name) {
			name = _name;
		}

		public LibraryConfig(string _name, DirectoryInfo _path) {
			name = _name;
			sourcePath = _path;
		}

		public LibraryConfig(string _name, FileInfo _path) {
			name = _name;
			binaryPath = _path;
		}

		public void Load(XmlElement libConfigEl) {
			string sourcePathString = GetElementString(libConfigEl, "path");
			if (sourcePathString != null) {
				if (sourcePathString.EndsWith(".dll")) {
					sourcePath = null;
					binaryPath = new FileInfo(sourcePathString);
				} else {
					sourcePath = new DirectoryInfo(sourcePathString);
					binaryPath = null;
				}
			}

			ignoreSources = CollectStringArray(libConfigEl, "ignore-source", "name");
			ignoreTypes = CollectStringArray(libConfigEl, "ignore-type", "name");
			overlays = LowerStringArray(CollectStringArray(libConfigEl, "overlay", "name"));
			depends = LowerStringArray(CollectStringArray(libConfigEl, "depends", "name"));

			string disabledString = libConfigEl.GetAttribute("disabled");
			disabled = disabledString != null && disabledString != ""
				&& Boolean.Parse(disabledString);

			string warnString = libConfigEl.GetAttribute("warn");
			if (warnString != null && warnString != "")
				warningLevel = Int32.Parse(warnString);
		}

		public string Name {
			get { return name; }
		}
		public DirectoryInfo SourcePath {
			get { return sourcePath; }
		}
		public FileInfo BinaryPath {
			get { return binaryPath; }
		}
		public bool Exists {
			get {
				return (sourcePath != null && sourcePath.Exists) ||
					(binaryPath != null && binaryPath.Exists);
			}
		}
		public bool Disabled {
			get { return disabled; }
			set { disabled = value; }
		}
		public int WarningLevel {
			get { return warningLevel; }
		}
		public string[] Overlays {
			get { return overlays; }
		}
		public string[] Depends {
			get { return depends; }
		}

		public bool GetIgnoreSource(string filename) {
			if (ignoreSources == null)
				return false;

			foreach (string ign in ignoreSources)
				/* XXX: better check */
				if (filename.EndsWith(ign))
					return true;

			return false;
		}
		public bool GetIgnoreType(Type type) {
			if (ignoreTypes == null)
				return false;

			foreach (string ign in ignoreTypes)
				if (ign == type.FullName)
					return true;

			return false;
		}
	}

	public class LoginConfig {
		private bool ignoreAuthID, autoCreateAccounts;
		private string accountDatabase;
		private Hashtable superClients = new Hashtable();

		public LoginConfig() {
		}

		public LoginConfig(XmlElement el) {
			ignoreAuthID = Config.GetElementBool(el, "ignore-auth-id", false);
			autoCreateAccounts = Config.GetElementBool(el, "auto-create-accounts", false);
			accountDatabase = GetElementString(el, "account-database");

			foreach (XmlElement priv in el.GetElementsByTagName("super-client")) {
				string ip = priv.InnerText;
				if (ip == null)
					continue;
				ip = ip.Trim();
				if (ip == "")
					continue;
				superClients[ip] = true;
			}
		}

		private static string GetElementString(XmlElement parent, string tag) {
			XmlNodeList nl = parent.GetElementsByTagName(tag);
			if (nl.Count == 0)
				return null;
			return nl[0].InnerText;
		}

		public bool IgnoreAuthID {
			get { return ignoreAuthID; }
		}

		public bool AutoCreateAccounts {
			get { return autoCreateAccounts; }
		}

		public string AccountDatabase {
			get { return accountDatabase; }
		}

		public bool IsSuperClient(string ip) {
			return ip != null && superClients.ContainsKey(ip);
		}
	}

	public class GameServerConfig {
		private String name;
		private IPEndPoint address;
		private bool sendAuthID;

		public GameServerConfig(String _name, IPEndPoint _address,
								bool _sendAuthID) {
			name = _name;
			address = _address;
			sendAuthID = _sendAuthID;
		}

		public String Name {
			get { return name; }
		}

		public IPEndPoint Address {
			get { return address; }
		}

		public bool SendAuthID {
			get { return sendAuthID; }
		}
	}

	public class GameServerListConfig {
		private ArrayList servers = new ArrayList();

		public GameServerListConfig() {
		}

		public GameServerListConfig(XmlElement el) {
			foreach (XmlElement gs in el.GetElementsByTagName("game-server")) {
				string name = GetElementString(gs, "name");
				if (name == null) {
					Console.WriteLine("Game server without name ignored");
					continue;
				}

				string addressString = GetElementString(gs, "address");
				if (addressString == null) {
					Console.WriteLine("Game server without address ignored");
					continue;
				}

				string[] splitted = addressString.Split(new char[]{':'}, 2);
				if (splitted.Length != 2) {
					Console.WriteLine("Game server without port ignored");
					continue;
				}

				IPAddress ip;
				try {
					IPHostEntry he = Dns.Resolve(splitted[0]);
					if (he.AddressList.Length == 0) {
						Console.WriteLine("Failed to resolve {0}", splitted[0]);
						continue;
					}
					ip = he.AddressList[he.AddressList.Length - 1];
				} catch (Exception e) {
					Console.WriteLine("Failed to resolve {0}: {1}", splitted[0], e);
					continue;
				}

				short port;
				try {
					port = Int16.Parse(splitted[1]);
				} catch {
					Console.WriteLine("Invalid game server port ignored");
					continue;
				}

				IPEndPoint address = new IPEndPoint(ip, port);

				bool sendAuthID = Config.GetElementBool(gs, "send-auth-id", false);

				servers.Add(new GameServerConfig(name, address, sendAuthID));
			}
		}

		private static string GetElementString(XmlElement parent, string tag) {
			XmlNodeList nl = parent.GetElementsByTagName(tag);
			if (nl.Count == 0)
				return null;
			return nl[0].InnerText;
		}

		public IEnumerable GameServers {
			get { return servers; }
		}

		public GameServerConfig this[string name] {
			get {
				foreach (GameServerConfig gs in servers) {
					if (gs.Name == name)
						return gs;
				}
				return null;
			}
		}
	}

	public class Config {
		private string filename;
		private XmlDocument document;
		private bool multiThreading;
		private ArrayList dataDirectories;
		private Hashtable libraryConfig = new Hashtable();
		private LoginConfig loginConfig;
		private GameServerListConfig gameServerListConfig;

		public Config(string _filename) {
			filename = _filename;

			Defaults();
			Load();
		}

		public bool Exists {
			get { return File.Exists(filename); }
		}

		public bool MultiThreading {
			get { return multiThreading; }
		}

		public ArrayList DataDirectories {
			get { return dataDirectories; }
		}

		public LibraryConfig GetLibraryConfig(string name) {
			return (LibraryConfig)libraryConfig[name];
		}
		public ICollection Libraries {
			get { return libraryConfig.Values; }
		}

		public LoginConfig LoginConfig {
			get { return loginConfig; }
		}

		public GameServerListConfig GameServerListConfig {
			get { return gameServerListConfig; }
		}

		public XmlElement GetConfiguration(string path) {
			XmlElement el = document.DocumentElement;

			foreach (string seg in path.Split('/')) {
				XmlElement child = (XmlElement)el.SelectSingleNode(seg);
				if (child == null) {
					child = document.CreateElement(seg);
					el.AppendChild(child);
				}
				el = child;
			}

			return el;
		}

		private void Defaults() {
			LibraryConfig coreConfig = new LibraryConfig("core");
			libraryConfig["core"] = coreConfig;

			LibraryConfig legacyConfig;
			DirectoryInfo legacy = new DirectoryInfo(Path.Combine(Core.BaseDirectory,
																  "Scripts"));
			if (legacy.Exists) {
				legacyConfig = new LibraryConfig("legacy", legacy);
				libraryConfig[legacyConfig.Name] = legacyConfig;
			}

			DirectoryInfo local = Core.BaseDirectoryInfo
				.CreateSubdirectory("local");

			DirectoryInfo src = local
				.CreateSubdirectory("src");
			foreach (DirectoryInfo sub in src.GetDirectories()) {
				string libName = sub.Name.ToLower();
				if (libName == "core" || libName == "legacy") {
					Console.WriteLine("Warning: the library name '{0}' is invalid",
									  libName);
					continue;
				}

				libraryConfig[libName] = new LibraryConfig(libName, sub);
			}

			DirectoryInfo lib = local
				.CreateSubdirectory("lib");
			foreach (FileInfo libFile in lib.GetFiles("*.dll")) {
				string fileName = libFile.Name;
				string libName = fileName.Substring(0, fileName.Length - 4).ToLower();

				if (libName == "core" || libName == "legacy") {
					Console.WriteLine("Warning: the library name '{0}' is invalid",
									  libName);
					continue;
				}

				if (libraryConfig.ContainsKey(libName)) {
					Console.WriteLine("Warning: duplicate library '{0}' in '{1}'",
									  libName, libFile);
					continue;
				}

				libraryConfig[libName] = new LibraryConfig(libName, libFile);
			}
		}

		public static bool GetElementBool(XmlElement parent, string tag,
										  bool defaultValue) {
			if (parent == null)
				return defaultValue;
			XmlNodeList nl = parent.GetElementsByTagName(tag);
			if (nl.Count == 0)
				return defaultValue;
			string value = ((XmlElement)nl[0]).GetAttribute("value");
			if (value == null || value == "")
				return true;
			return value == "on" || value == "true" || value == "yes";
		}

		public static void RemoveElement(XmlElement parent, string tag) {
			if (parent == null)
				return;
			XmlNodeList nl = parent.GetElementsByTagName(tag);
			XmlNode[] children = new XmlNode[nl.Count];
			for (int i = 0; i < children.Length; i++)
				children[i] = nl.Item(i);
			foreach (XmlNode child in children)
				parent.RemoveChild(child);
		}

		public static void SetElementBool(XmlElement parent, string tag,
										  bool value) {
			XmlElement el;

			RemoveElement(parent, tag);

			el = parent.OwnerDocument.CreateElement(tag);
			el.SetAttribute("value", value ? "on" : "off");
			parent.AppendChild(el);
		}

		private void Load() {
			document = new XmlDocument();
			dataDirectories = new ArrayList();

			if (Exists) {
				XmlTextReader reader = new XmlTextReader(filename);
				try {
					document.Load(reader);
				} finally {
					reader.Close();
				}
			} else {
				document.AppendChild(document.CreateElement("sunuo-config"));
			}

			// section "global"
			XmlElement global = GetConfiguration("global");
			if (global != null) {
				multiThreading = GetElementBool(global, "multi-threading", false);
			}

			// section "locations"
			XmlElement locations = GetConfiguration("locations");
			foreach (XmlElement dp in locations.GetElementsByTagName("data-path")) {
				string path = dp.InnerText;
				if (Directory.Exists(path))
					dataDirectories.Add(path);
			}

			// section "libraries"
			XmlElement librariesEl = GetConfiguration("libraries");
			foreach (XmlElement el in librariesEl.GetElementsByTagName("library")) {
				string name = el.GetAttribute("name");
				if (name == null || name == "") {
					Console.WriteLine("Warning: library element without name attribute");
					continue;
				}

				name = name.ToLower();

				LibraryConfig libConfig = (LibraryConfig)libraryConfig[name];
				if (libConfig == null)
					libraryConfig[name] = libConfig = new LibraryConfig(name);

				libConfig.Load(el);
			}

			if (!libraryConfig.ContainsKey("legacy"))
				libraryConfig["legacy"] = new LibraryConfig("legacy");

			// section "login"
			XmlElement loginEl = GetConfiguration("login");
			loginConfig = loginEl == null
				? new LoginConfig()
				: new LoginConfig(loginEl);

			// section "server-list"
			XmlElement serverListEl = GetConfiguration("server-list");
			if (serverListEl != null)
				gameServerListConfig = new GameServerListConfig(serverListEl);
		}

		public void Save() {
			string tempFilename;
			if (filename.EndsWith(".xml")) {
				tempFilename = filename.Substring(0, filename.Length - 4) + ".new";
			} else {
				tempFilename = filename + ".new";
			}

			// section "global"
			XmlElement global = GetConfiguration("global");

			SetElementBool(global, "multi-threading", multiThreading);

			// section "locations"
			XmlElement locations = GetConfiguration("locations");
			RemoveElement(locations, "data-path");

			Hashtable dirHash = new Hashtable();

			foreach (string path in	dataDirectories) {
				/* check for double path */
				if (dirHash.ContainsKey(path))
					continue;
				dirHash[path] = true;

				XmlElement el = document.CreateElement("data-path");
				el.InnerText = path;
				locations.AppendChild(el);
			}

			// section "login"
			XmlElement login = GetConfiguration("login");

			SetElementBool(login, "ignore-auth-id", loginConfig.IgnoreAuthID);

			// write to file
			XmlTextWriter writer = new XmlTextWriter(tempFilename, System.Text.Encoding.UTF8);
			writer.Formatting = Formatting.Indented;

			try {
				document.Save(writer);
				writer.Close();
				File.Delete(filename);
				File.Move(tempFilename, filename);
			} catch {
				writer.Close();
				File.Delete(tempFilename);
				throw;
			}
		}
	}
}
