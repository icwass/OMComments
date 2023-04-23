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

using OMDraw = class_135;
using Font = class_1;
//using PartType = class_139;
//using Permissions = enum_149;
//using PartTypes = class_191;
//using Texture = class_256;
public class CommentEditor : IScreen
{
	//=========================//
	//boilerplate needed as a subclass of IScreen
	public bool method_1037() => false;
	public void method_47(bool param_5434) { }
	public void method_48() { }
	public void method_50(float timeDelta) { MainFunction(); }
	//=========================//
	// helpers
	public static readonly Encoding CommentEncoding = Encoding.Unicode;
	public static readonly Font Title = class_238.field_1990.field_2146;
	public static readonly Font Text = class_238.field_1990.field_2143;
	public static readonly Font SubTitle = class_238.field_1990.field_2145;

	public static readonly Color TextColor = class_181.field_1718;

	public CommentEditor(SolutionEditorScreen ses, Part commentPart)
	{
		this.ses = ses;
		this.commentPart = commentPart;

		//data
		pointerLength = commentPart.method_1167();
		commentText = loadCommentFromPipes(commentPart);
	}
	private static string loadCommentFromPipes(Part commentPart)
	{
		int arrayLength = commentPart.field_2703;
		if (arrayLength == 0) return "";

		var pipeHexes = commentPart.method_1173();
		byte[] buffer = new byte[pipeHexes.Count * 8];
		for (int i = 0; i < pipeHexes.Count; i++)
		{
			int Q = pipeHexes[i].Q;
			int R = pipeHexes[i].R;
			buffer[i*8 + 0] = (byte)(Q & 0xFF);
			buffer[i*8 + 1] = (byte)((Q>>8) & 0xFF);
			buffer[i*8 + 2] = (byte)((Q>>16) & 0xFF);
			buffer[i*8 + 3] = (byte)((Q>>24) & 0xFF);
			buffer[i*8 + 4] = (byte)(R & 0xFF);
			buffer[i*8 + 5] = (byte)((R>>8) & 0xFF);
			buffer[i*8 + 6] = (byte)((R>>16) & 0xFF);
			buffer[i*8 + 7] = (byte)((R>>24) & 0xFF);
		}

		byte[] bytes = new byte[arrayLength];
		for (int i = 0; i < arrayLength; i++)
		{
			bytes[i] = buffer[i];
		}
		return CommentEncoding.GetString(bytes);
	}
	private void saveCommentIntoPipes(string comment)
	{
		List<byte> bytes = CommentEncoding.GetBytes(comment).ToList();

		for (int i = 0; i < bytes.Count; i++)
		{
			Logger.Log("" + i + " : " + bytes[i]);
		}


		int arrayLength = bytes.Count;
		this.commentPart.field_2703 = arrayLength;

		while (bytes.Count % 8 != 0)
		{
			bytes.Add(0);
		}
		var pipeHexes = new List<HexIndex>();
		for (int i = 0; i < bytes.Count; i += 8)
		{
			int Q = (bytes[i + 3] << 24) + (bytes[i + 2] << 16) + (bytes[i + 1] << 8) + bytes[i + 0];
			int R = (bytes[i + 7] << 24) + (bytes[i + 6] << 16) + (bytes[i + 5] << 8) + bytes[i + 4];
			pipeHexes.Add(new HexIndex(Q,R));
		}
		new DynamicData(this.commentPart).Set("field_2704", pipeHexes);
	}
	private void saveDataIntoPart()
	{
		saveCommentIntoPipes(commentText);
		var part_dyn = new DynamicData(commentPart);
		part_dyn.Set("field_2698", pointerLength);

		ses.method_2108(); // add undo checkpoint, save solution to file
	}
	//=========================//
	public static Bounds2 DrawText(string text, Vector2 pos, Font font, Color color, TextAlignment alignment, float maxWidth = float.MaxValue, float maxHeight = float.MaxValue)
	{
		return OMDraw.method_290(text, pos, font, color, (enum_0)(int)alignment, 1f, 0.6f, maxWidth, maxHeight, 0, new Color(), null, int.MaxValue, true, true);
	}

	public static void DrawSectionHeader(string name, Vector2 pos)
	{
		DrawText(name + " : ", pos, Title, TextColor, TextAlignment.Centred);
	}

	public static void DrawStringField(string name, string value, Vector2 pos)
	{
		DrawText(name + " : ", pos, SubTitle, TextColor, TextAlignment.Right);
		DrawText(" " + value, pos, SubTitle, TextColor, TextAlignment.Left);
	}
	public static void fetchCommentStrings(SolutionEditorScreen screen_self, out string header, out string comment)
	{
		header = "";
		comment = "";

		Part commentPart;
		if (!MainClass.fetchCommentAtMouse(screen_self, out commentPart)) return;

		header = "Comment";
		comment = loadCommentFromPipes(commentPart);
	}


	//=========================//
	// "real" stuff
	SolutionEditorScreen ses;
	Part commentPart;

	//data
	int pointerLength;
	string commentText = "";

	void MainFunction()
	{
		Vector2 size = new(1000f, 922f);
		Vector2 pos = (Input.ScreenSize() / 2 - size / 2).Rounded();
		Vector2 bgPos = pos + new Vector2(78f, 88f);
		Vector2 bgSize = size + new Vector2(-152f, -158f);

		UI.DrawUiBackground(bgPos, bgSize);
		UI.DrawUiFrame(pos, size);
		UI.DrawHeader("Comment Editor", pos + new Vector2(100f, size.Y - 99f), (int) size.X - 300, true, true);

		if (UI.DrawAndCheckCloseButton(pos, size, new Vector2(104, 98)))
		{
			saveDataIntoPart();
			UI.HandleCloseButton();
		}

		var part = commentPart;
		var part_dyn = new DynamicData(commentPart);


		Vector2 yoffset = new Vector2(0f, 30f);
		Vector2 position = bgPos + bgSize / 2 + yoffset*5;

		position -= yoffset; DrawStringField("Pointer Length", "" + pointerLength, position);

		position -= new Vector2(100f, 0f);
		position -= 2 * yoffset;
		if (UI.DrawAndCheckBoxButton("[+] Pointer Length", position))
		{
			pointerLength++;
		}
		position -= 2 * yoffset;
		if (UI.DrawAndCheckBoxButton("[-] Pointer Length", position) && pointerLength > 0)
		{
			pointerLength--;
		}

		position -= new Vector2(100f, 0f);
		position -= 2 * yoffset;

		position -= yoffset; DrawStringField("commentText : ", commentText, position);

		Vector2 vector2_1 = new Vector2(1516f, 922f);
		Vector2 vector2_2 = (class_115.field_1433 / 2 - vector2_1 / 2 + new Vector2(-2f, -11f)).Rounded();
		Bounds2 bounds2 = Bounds2.WithSize(vector2_2 + new Vector2(598f, 93f), new Vector2(840f, 344f));

		ButtonDrawingLogic buttonDrawingLogic = class_140.method_313((string)class_134.method_253("Edit", string.Empty), vector2_2 + new Vector2(783f, 100f), 120, 46);
		if (buttonDrawingLogic.method_824(true, true))
		{
			var editBox = MessageBoxScreen.method_1096(bounds2, false, (string)class_134.method_253("Please edit your comment:", string.Empty), commentText, (string)class_134.method_253("Save Changes", string.Empty), new Action<string>(x => { commentText = x; }));
			GameLogic.field_2434.method_946(editBox);
			class_238.field_1991.field_1821.method_28(1f);
		}





	}
}
