using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace OMComments;

using PartType = class_139;
using Permissions = enum_149;
using PartTypes = class_191;
using Texture = class_256;
public class MainClass : QuintessentialMod
{
	public static PartType Comment;
	private static LocString commentName, commentDesc, commentNull;
	public static bool fetchCommentAtMouse(SolutionEditorScreen ses, out Part commentPart)
	{
		commentPart = default(Part);
		Maybe<Part> maybe = ses.method_2011(Input.MousePos(), ses.field_4009);
		if (!maybe.method_1085()) return false;
		if (maybe.method_1087().method_1159() != Comment) return false;

		commentPart = maybe.method_1087();
		return true;
	}

	//=============================================================================================================================//
	// load content
	public override void LoadPuzzleContent()
	{
		commentName = class_134.method_253("Glyph of Commentary", string.Empty);
		commentDesc = class_134.method_253("An alchemist can engrave text and symbols onto this glyph to provide commentary on, say, the thought process behind their design.\n\n[Right-click this part to open the editor.]", string.Empty);
		commentNull = class_134.method_253("OMComments Null string :: This should never be used or drawn in-game, but is used to signal method_2107 to fetch the comment part's contents", string.Empty);
		Comment = new PartType()
		{
			/*ID*/field_1528 = "glyph-comment",
			/*Name*/field_1529 = commentNull,
			/*Desc*/field_1530 = commentNull,
			/*Force-rotatable*/field_1536 = true,
			/*Is a Glyph?*/field_1539 = true,
			/*Hex Footprint*/field_1540 = new HexIndex[1] { new HexIndex(0, 0) },
			/*Icon*/field_1547 = class_238.field_1989.field_90.field_245.field_309, // equilibrium
			/*Hover Icon*/field_1548 = class_238.field_1989.field_90.field_245.field_310, // equilibrium
			/*Glow (Shadow)*/field_1549 = class_238.field_1989.field_97.field_382, // single hex
			/*Stroke (Outline)*/field_1550 = class_238.field_1989.field_97.field_383, // single hex
			/*Permissions*/field_1551 = Permissions.None,
		};
		void commentDrawing(Part part, Vector2 pos, SolutionEditorBase editor, class_195 renderer)
		{
			var part_dyn = new DynamicData(part);
			if (part_dyn.Get<List<HexIndex>>("field_2704") == null) part_dyn.Set("field_2704", new List<HexIndex>());
			if (part.field_2705 == null) part.field_2705 = new List<struct_36>();

			int pointerLength = part_dyn.Get<int>("field_2694");


			Texture marker_base = class_238.field_1989.field_90.field_185;
			Texture marker_lighting = class_238.field_1989.field_90.field_187;

			Vector2 vector2 = (marker_base.field_2056.ToVector2() / 2).Rounded() + new Vector2(0.0f, 1f);
			renderer.method_521(marker_base, vector2);
			renderer.method_528(marker_lighting, new HexIndex(0, 0), Vector2.Zero);
			//renderer.method_521(class_238.field_1989.field_90.field_186, vector2);













		}
		QApi.AddPartType(Comment, commentDrawing);
		QApi.AddPartTypeToPanel(Comment, PartTypes.field_1782);//inserts part type after Equilibrium in the parts tray
	}
	public override void Unload() { }
	//=============================================================================================================================//
	// hooking
	public override void Load()
	{
		// changes needed to save/load comment parts correctly
		On.Part.method_1175 += ClonePart;
		On.Solution.method_1959 += SolutionWriter;
		On.Solution.method_1960 += SolutionReader;
	}
	public override void PostLoad()
	{
		// changed needed to prevent comments from adding to the footprint
		On.Solution.method_1947 += Method_1947;
		// changes needed to draw comment text-boxes correctly
		On.SolutionEditorScreen.method_2107 += Method_2107;
		On.SolutionEditorPartsPanel.PartsSection.method_2052 += SEPP_PS_method_2052;
		On.SolutionEditorScreen.method_50 += SolutionEditorScreen_Method_50;
		// changes needed to draw CommentPointers
		On.class_153.method_221 += c153_Method_221;
	}
	//=========================//
	public T CommentWrapper<T>(Func<T> func)
	{
		// run function while pretending that Comment parts have the IsConduit flag
		Comment.field_1543 = true;
		var ret = func();
		Comment.field_1543 = false;
		return ret;
	}
	public Part ClonePart(On.Part.orig_method_1175 orig, Part part_self, Solution solution, Maybe<Part> maybePart) => CommentWrapper(() => orig(part_self, solution, maybePart));
	public byte[] SolutionWriter(On.Solution.orig_method_1959 orig, Solution solution_self) => CommentWrapper(() => orig(solution_self));
	public Maybe<Solution> SolutionReader(On.Solution.orig_method_1960 orig, byte[] stream, SolutionNameOnDisk name, DateTime time) => CommentWrapper(() => orig(stream, name, time));
	//=========================//
	public static HashSet<HexIndex> Method_1947(On.Solution.orig_method_1947 orig, Solution solution_self, Maybe<Part> maybePart, enum_137 enum137)
	{
		var partsList = solution_self.field_3919;
		if (!maybePart.method_1085() && enum137 == 0)
		{
			//don't include Comments in the foot print if we're counting area
			solution_self.field_3919 = partsList.Where(x => x.method_1159() != Comment).ToList();
		}
		var ret = orig(solution_self, maybePart, enum137);
		solution_self.field_3919 = partsList;
		return ret;
	}
	//=========================//
	public void Method_2107(On.SolutionEditorScreen.orig_method_2107 orig, SolutionEditorScreen screen_self, string name, string desc, Maybe<SDL.enum_160> hotkey)
	{
		// for comments, draw the comment's contents instead of the PartType name and description
		if (name == commentNull && desc == commentNull && !hotkey.method_1085())
		{
			CommentEditor.fetchCommentStrings(screen_self, out name, out desc);
		}
		orig(screen_self, name, desc, hotkey);
	}
	public void SEPP_PS_method_2052(
		On.SolutionEditorPartsPanel.PartsSection.orig_method_2052 orig,
		SolutionEditorPartsPanel.PartsSection seppps_self,
		bool param_5651,
		Maybe<PartTypeForToolbar> maybePTFT,
		SolutionEditorScreen ses)
	{
		// comments have a different hover-description in the parts tray
		Comment.field_1529 = commentName;
		Comment.field_1530 = commentDesc;
		orig(seppps_self, param_5651, maybePTFT, ses);
		Comment.field_1529 = commentNull;
		Comment.field_1530 = commentNull;
	}

	public void SolutionEditorScreen_Method_50(On.SolutionEditorScreen.orig_method_50 orig, SolutionEditorScreen screen_self, float timeDelta)
	{
		if (Input.IsRightClickPressed() && !Input.IsControlHeld() && !Input.IsShiftHeld() && !Input.IsAltHeld() && screen_self.method_503() == enum_128.Stopped)
		{
			Part commentPart;
			if (fetchCommentAtMouse(screen_self, out commentPart))
			{
				UI.OpenScreen(new CommentEditor(screen_self, commentPart));
				class_238.field_1991.field_1821.method_28(1f); // sound/click_button
				return;
			}
		}

		orig(screen_self, timeDelta);

		// display comments if hovering over a comment part while the sim is running
		if (screen_self.method_503() == enum_128.Stopped) return;
		if (!fetchCommentAtMouse(screen_self, out _)) return;

		string header = commentNull;
		string comment = commentNull;
		CommentEditor.fetchCommentStrings(screen_self, out header, out comment);
		if (header == "" || comment == "") return;

		// create the string to draw - this code is based on method_2107
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(header.method_441());
		stringBuilder.AppendLine();
		stringBuilder.AppendLine();
		stringBuilder.Append(comment);
		var field4031 = (Maybe<string>)stringBuilder.ToString();

		// draw the string - this code is based on method_50
		Vector2 vector2_4 = class_115.method_202() + (new DynamicData(screen_self).Get<bool>("field_4034") ? new Vector2(160f, 11f) : new Vector2(65f, 0.0f));
		Bounds2 bounds2_3 = class_135.method_291(field4031.method_1087(), vector2_4, class_238.field_1990.field_2143, (enum_0)0, 1f, 0.6f, 330f, float.MaxValue);
		Bounds2 bounds2_4 = Bounds2.WithCorners(bounds2_3.Min.X - 22f, bounds2_3.Min.Y - 22f, 0.0f, bounds2_3.Max.Y + 12f);
		bounds2_4.Max.X = bounds2_4.Min.X + class_238.field_1989.field_101.field_795.method_688();
		Vector2 vector2_5 = vector2_4 - bounds2_4.Min;
		if (bounds2_4.Min.X < 10.0)
			bounds2_4 = bounds2_4.Translated(new Vector2(10f - bounds2_4.Min.X, 0.0f));
		if (bounds2_4.Max.X > class_115.field_1433.X - 10.0)
			bounds2_4 = bounds2_4.Translated(new Vector2(class_115.field_1433.X - 10f - bounds2_4.Max.X, 0.0f));
		if (bounds2_4.Min.Y < 10.0)
			bounds2_4 = bounds2_4.Translated(new Vector2(0.0f, 10f - bounds2_4.Min.Y));
		if (bounds2_4.Contains(class_115.method_202()))
			bounds2_4 = bounds2_4.Translated(new Vector2(0.0f, class_115.method_202().Y - bounds2_4.Min.Y + 10f));
		if (bounds2_4.Max.Y > class_115.field_1433.Y - 10.0)
			bounds2_4 = bounds2_4.Translated(new Vector2(0.0f, class_115.field_1433.Y - 10f - bounds2_4.Max.Y));
		class_135.method_275(class_238.field_1989.field_101.field_795, Color.White, bounds2_4);
		class_135.method_290(field4031.method_1087(), bounds2_4.Min + vector2_5, class_238.field_1990.field_2143, class_181.field_1719, (enum_0)0, 1f, 0.6f, 330f, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue, false, true);
		if (field4031.method_1087().Contains("\n\n"))
			class_135.method_272(class_238.field_1989.field_101.field_794, bounds2_4.Min + vector2_5 + new Vector2(0.0f, -11f));
	}

	public static void c153_Method_221(On.class_153.orig_method_221 orig, class_153 c153_self, float param_3616)
	{
		orig(c153_self, param_3616);

		// draw CommentPointer, if applicable
		var ses = new DynamicData(c153_self).Get<SolutionEditorScreen>("field_2007");

		Part commentPart;
		if (!fetchCommentAtMouse(ses, out commentPart)) return;

		var hex = commentPart.method_1161();
		var rot = commentPart.method_1163();
		var pointerLength = commentPart.method_1167();

		if (pointerLength > 0)
		{
			Vector2 view = c153_self.method_359();
			Texture tex = class_238.field_1989.field_82.field_647; // hex_emphasis
			Texture pulse = class_238.field_1989.field_82.field_648; // hex_emphasis_pulse
			float a = class_162.method_415((float)Math.Cos((double)Time.NowInSeconds() * 3.0), -1f, 1f, 0.3f, 1f);
			for (int i = 1; i <= pointerLength; i++)
			{
				var highlightHex = new HexIndex(i, 0).Rotated(rot) + hex;
				Vector2 pos = class_187.field_1742.method_491(highlightHex, view) + new Vector2(-40f, -47f);

				class_135.method_271(pulse, Color.White.WithAlpha(a), pos);
				if (i == pointerLength)
				{
					class_135.method_271(pulse, Color.White.WithAlpha(a), pos);
					class_135.method_272(tex, pos);
					class_135.method_272(tex, pos);
				}
			}
		}
	}
}
