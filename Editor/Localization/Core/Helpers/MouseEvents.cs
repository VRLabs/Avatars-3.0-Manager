using UnityEngine;

namespace DreadScripts.Localization
{
	public static class MouseEvents
	{
		public static bool OnHoverEnter(Rect r, ref bool b)
		{
			Event e = Event.current;
			if (!r.Contains(e.mousePosition)) b = true;
			else if (b) return !(b = false);

			return false;
		}

		public static bool OnLeftClick(Rect r)
		{
			Event e = Event.current;
			if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
			{
				e.Use();
				return true;
			}

			return false;
		}

		public static bool OnContextClick(Rect r)
		{
			Event e = Event.current;
			if (e.type == EventType.ContextClick && r.Contains(e.mousePosition))
			{
				e.Use();
				return true;
			}

			return false;
		}
	}
}