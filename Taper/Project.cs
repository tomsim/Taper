﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Taper
{
    class Project
    {
        
        public static string name = "";                     //Имя проекта
        public static bool changed;                         //Флаг изменения
        public static List<Block> TAP = new List<Block>();  //Собственно TAP-файл
        public static Block view;                           //Блок для просмотрщика
        public static string rename = "";                   //Пока не помню сути этой переменной.....

        //Временно эти поля публичные, потом перенесу управление историей сюда
        public static List<List<Block>> history = new List<List<Block>>();  //История изменений проекта
        public static int hIndex;                           //Позиция в истории
        static List<Block> Buffer = new List<Block>(); //Буфер обмена

        /// <summary>
        /// Создание нового проекта, запускается так же и перед открытием файла
        /// </summary>
        public static void New()
        {
            TAP.Clear();
            history.Clear();
            hIndex = 0;
            name = Program.FileUnnamed;
            changed = false;
        }

        public static void Open(string filename, bool add)
        {
            if (add) Change(); else New();
            try
            {
                System.IO.BinaryReader file = new System.IO.BinaryReader(new System.IO.FileStream(filename, System.IO.FileMode.Open));
                while (file.BaseStream.Position < file.BaseStream.Length)
                {
                    int LEN = file.ReadUInt16();
                    byte[] Bytes = file.ReadBytes(LEN);
                    Add(Bytes);
                }
                file.Close();
                if (!add) name = filename;
            }
            catch { Program.Error("Произошла ошибка при открытии файла."); }
        }

        public static void Save(string filename)
        {
            try
            {
                System.IO.BinaryWriter file = new System.IO.BinaryWriter(new System.IO.FileStream(filename, System.IO.FileMode.Create));
                foreach (Block block in TAP)
                {
                    //Сохраняем заголовок
                    if (block.FileTitle != null)
                    {
                        file.Write((UInt16)19);
                        file.Write(block.FileTitle);
                    }
                    //Сохраняем блок данных
                    if (block.FileData != null)
                    {
                        file.Write((UInt16)block.FileData.Count());
                        file.Write(block.FileData);
                    }
                }
                file.Close();
                name = filename;
                changed = false;
            }
            catch { Program.Error("Произошла ошибка во время сохранения файла. Файл не сохранён."); }
        }

        /// <summary>
        /// Вызывается при изменениях в проекте. Меняет флаг и делает отмену
        /// </summary>
        public static void Change()
        {
            //Если индекс истории меньше чем размер истории, удаляем последующие моменты
            while (hIndex < history.Count) history.RemoveAt(history.Count - 1);
            List<Block> temp = new List<Block>();
            foreach (Block block in TAP)
                temp.Add(block);
            history.Add(temp);
            hIndex++;
            changed = true;
        }

        /// <summary>
        /// Добавление блока в проект
        /// </summary>
        /// <param name="Bytes"></param>
        public static void Add(byte[] Bytes)
        {
            TAP.Add(new Block(Bytes));
        }

        /// <summary>
        /// Собрать блоки по файлам
        /// </summary>
        public static void ListFiles()
        {
            List<Block> tempfile = new List<Block>();
            foreach (Block block in TAP)
                tempfile.Add(block);
            TAP.Clear();
            for (int i = 0; i < tempfile.Count(); i++)
            {
                if (tempfile[i].FileTitle != null & tempfile[i].FileData == null & i < tempfile.Count() - 1 &&
                    tempfile[i + 1].FileTitle == null & tempfile[i + 1].FileData != null)
                {
                    TAP.Add(new Block(tempfile[i].FileTitle));
                    TAP[TAP.Count() - 1].AddBlock(tempfile[i + 1].FileData);
                    i++;
                }
                else
                    TAP.Add(tempfile[i]);
            }
        }

        /// <summary>
        /// Разбить файлы на блоки
        /// </summary>
        public static void ListBlocks()
        {
            List<Block> tempfile = new List<Block>();
            foreach (Block block in TAP)
                tempfile.Add(block);
            TAP.Clear();
            foreach (Block block in tempfile)
            {
                if (block.FileTitle != null)
                    TAP.Add(new Block(block.FileTitle));
                if (block.FileData != null)
                    TAP.Add(new Block(block.FileData));
            }
        }

        /// <summary>
        /// Отменить
        /// </summary>
        public static void Undo()
        {
            if (hIndex < 2) return;
            hIndex--;
            TAP.Clear();
            foreach (Block block in history[hIndex - 1])
                TAP.Add(block);
            changed = true;
        }

        /// <summary>
        /// Вернуть
        /// </summary>
        public static void Redo()
        {
            if (hIndex == history.Count) return;
            hIndex++;
            TAP.Clear();
            foreach (Block block in history[Project.hIndex - 1])
                TAP.Add(block);
            changed = true;
        }

        /// <summary>
        /// Вырезать
        /// </summary>
        public static void Cut(ListView.SelectedIndexCollection selected)
        {
            if (selected.Count == 0) return;
            Change();
            Buffer.Clear();
            for (int i = 0; i < selected.Count; i++)
                Buffer.Add(TAP[selected[i]]);
            for (int i = selected.Count - 1; i >= 0; i--)
                TAP.RemoveAt(selected[i]);
        }

        /// <summary>
        /// Копирование
        /// </summary>
        public static void Copy(ListView.SelectedIndexCollection selected)
        {
            if (selected.Count == 0) return;
            Buffer.Clear();
            for (int i = 0; i < selected.Count; i++)
                Buffer.Add(TAP[selected[i]]);
        }

        /// <summary>
        /// Вставить
        /// </summary>
        public static void Paste(ListView.SelectedIndexCollection selected)
        {
            if (Buffer.Count == 0) return;
            Change();
            if (selected.Count == 0)
            {
                foreach (Block block in Buffer)
                    TAP.Add(block);
            }
            else
            {
                //Там-то было легко... только добавить к концу, здесь же сначала надо раздвинуть блоки
                for (int i = 0; i < Buffer.Count; i++)
                    TAP.Add(null);
                for (int i = TAP.Count - 1; i >= selected[0] + Buffer.Count; i--)
                    TAP[i] = TAP[i - Buffer.Count];
                for (int i = 0; i < Buffer.Count; i++)
                    TAP[selected[0] + i] = Buffer[i];
            }
        }

    }
}
