using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.github.pandrabox.pandravase.editor;
using UnityEngine;

namespace com.github.pandrabox.flatsplus
{
    public static class MenuBuilderExtension
    {
        /// <summary>
        /// アイコンを設定する拡張メソッド
        /// </summary>
        /// <param name="menuBuilder">拡張対象のMenuBuilder</param>
        /// <param name="name">アイコン名（ファイル名のみ、拡張子不要）</param>
        /// <returns>MenuBuilder</returns>
        public static MenuBuilder Ico(this MenuBuilder menuBuilder, string name)
        {
            // アイコンのフルパスを構築（".png" を自動付加）
            string iconPath = Path.Combine("Packages/com.github.pandrabox.flatsplus/Assets/Icon", name + ".png");

            // アイコンをロード
            Texture2D icon = LoadTexture(iconPath);
            if (icon == null)
            {
                Debug.LogWarning($"指定されたアイコン '{name}.png' が見つかりませんでした。パス: {iconPath}");
                return menuBuilder;
            }

            // アイコンを設定
            menuBuilder.SetIco(icon);
            return menuBuilder;
        }

        /// <summary>
        /// 指定されたパスからTexture2Dをロードする
        /// </summary>
        /// <param name="path">アイコンのフルパス</param>
        /// <returns>ロードされたTexture2D</returns>
        private static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData))
            {
                return texture;
            }

            return null;
        }
    }
}