using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CSALMongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ToolArt
{
    public partial class Form1 : Form
    {

        //should change the name of the database
        public const string DB_URL = "mongodb://localhost:27017/csaldata";
        //protected MongoDatabase testDB = null;
        List<String> studentsInClass = new List<string>();

        List<String> needStudents = new List<string>();
        //List<String> needStudentsA = new List<string>();

        // Generate all the tags
        String Introduction = "\n";
        String Tags = "RecordID\tUserID\tClassID\tLessonID\tCompletion\tCorrectness";

        // Question number of each lesson
        int[] numOfQ = {20, 24, 18, 25, 11, 20, 20, 10, 23, 30, 13, 11, 5, 13, 32, 
                       12, 13, 19, 12, 20, 24, 16, 27, 18, 22, 14, 6, 20, 15, 14};

        public Form1()
        {
            InitializeComponent();

            /*
            // get tags of the data
            String lessonTags = "";
            for (int i = 1; i < 31; i++)
            {
                lessonTags += "\t" + i.ToString() + " Completion\t" + i.ToString() + "Correctness";
            }

            Tags += lessonTags;
            */

            this.richTextBox1.Text = Tags.ToString() + "\n";

            // Start 
            try
            {
                //query from the database
                var db = new CSALDatabase(DB_URL);
                var students = db.FindStudents();
                var lessons = db.FindLessons();
                var classes = db.FindClasses();

                // find the target classes
                foreach (var oneClass in classes)
                {
                    if (oneClass.ClassID == "aec" || oneClass.ClassID == "kingwilliam" || oneClass.ClassID == "main" || oneClass.ClassID == "tlp"
                        || oneClass.ClassID == "lai" || oneClass.ClassID == "marietta")
                    {
                        foreach (String student in oneClass.Students)
                        {
                            if (!String.IsNullOrWhiteSpace(student) && !String.IsNullOrWhiteSpace(oneClass.ClassID))
                            {
                                String cS = oneClass.ClassID + "-" + student;
                                needStudents.Add(cS);
                            }
                        }
                    }

                }

                // get question number for all the lessons
                List<List<int>> quesNum = getQuestionNumber(needStudents);


                //this.richTextBox1.Text = needStudentsA.Count.ToString();
                List<String> allRecords = getDetailInfo(needStudents, quesNum);

                
                String questionNum = "";
                for (int i = 0; i < 30; i++)
                {
                    questionNum += "\t" + quesNum[0][i].ToString() + "\t" + quesNum[1][i].ToString(); 
                }
                this.richTextBox1.AppendText("\n" + questionNum + "\n");
                
                int countRecord = 0;
                foreach (String record in allRecords)
                {
                    countRecord++;
                    this.richTextBox1.AppendText(countRecord.ToString() + "\t" + record + "\n");
                }

            }
            catch (Exception e)
            {
                e.GetBaseException();
                e.GetType();

            }
        }


        // all the information
        public List<String> getDetailInfo(List<String> studentsList, List<List<int>> quesNum)
        {
            List<String> allText = new List<String>();

            // try to get Detail Info
            foreach (var studentRecord in studentsList)
            {
                if (!String.IsNullOrWhiteSpace(studentRecord))
                {
                    // need to be careful here, may not right
                    List<String> perLesson = getInfoOne2(studentRecord, quesNum);

                    if (perLesson == null || perLesson.Count < 1)
                    {
                        continue;
                    }
                    else
                    {
                        foreach (String perRecord in perLesson)
                        {
                            allText.Add(perRecord);
                        }
                    }

                    //allText += recordCount.ToString() + "\t" + getInfoForOne(studentRecord) + "\n";
                }
            }

            return allText;
        }


        // get question number for each lesson
        public List<List<int>> getQuestionNumber(List<String> studentsList)
        {
            List<List<int>> quesNumList = new List<List<int>>();
            List<int> quesNumMax = new List<int>();
            List<int> quesNumMin = new List<int>();

            for (int i = 1; i < 31; i++)
            {
                int quesNum = 0, maxQues = 0, minQues = 0;
                Boolean flag = false;

                foreach (String studentRecord in studentsList)
                {
                    String classID = studentRecord.Split(new Char[] { '-' })[0];
                    String subjectID = studentRecord.Split(new Char[] { '-' })[1];

                    String allRecords = subjectID + "\t" + classID;

                    //2 | 4 | 8 | 10 | 11 | 12 | 13 | 14 | 18 | 26 | 27
                    var lessonId = "lesson" + i.ToString();

                    quesNum = getQuesNum(subjectID, lessonId);

                    if (quesNum != 0 && flag == false)
                    {
                        minQues = quesNum;
                        flag = true;
                    }
                    else if (quesNum == 0)
                    {
                        continue;
                    }
                    else if (quesNum < minQues)
                    {
                        minQues = quesNum;
                    }
                    else if (quesNum > maxQues)
                    {
                        maxQues = quesNum;
                    }
                }

                quesNumMax.Add(maxQues);
                quesNumMin.Add(minQues);
            }
            quesNumList.Add(quesNumMax);
            quesNumList.Add(quesNumMin);

            return quesNumList;
        }

        // for normal lesson 
        public int getQuesNum(String studentName, String lessonID)
        {
            var db = new CSALDatabase(DB_URL);
            var oneTurn = db.FindTurns(lessonID, studentName);
            int lastTurnID = 0, questionID = 0;

            if (oneTurn == null || oneTurn.Count < 1 || oneTurn[0].Turns.Count < 1)
            {
                return 0;
            }
            else
            {
                Boolean flag = false;

                foreach (var turn in oneTurn[0].Turns)
                {
                    // student tried more than 1, reset everything
                    if (turn.TurnID < lastTurnID)
                    {
                        questionID = 0;
                    }
                    else if (turn.Input.Event == "End")
                    {
                        return questionID;
                    }
                    else
                    {
                        foreach (var transition in turn.Transitions)
                        {

                            // analyze the correct/incorrect of the question
                            if (transition.RuleID == "Correct" || transition.RuleID == "Correct1")
                            {
                                // question ID should increase
                                questionID++;
                                flag = true;
                            }
                            else if (transition.RuleID == "Incorrect")
                            {
                                questionID++;
                                flag = true;
                            }
                            else if (transition.RuleID == "End")
                            {
                                return questionID;
                            }

                            foreach (var action in transition.Actions)
                            {
                                if (action.Act == "AddUserScore" && action.Data.Contains("1"))
                                {
                                    questionID++;
                                    flag = true;
                                    break;
                                }
                                else if (action.Act == "GetMediaFeedback" && action.Data.Contains("SAGoodAnswer"))
                                {
                                    questionID++;
                                    flag = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (flag == false)
                    {
                        if (turn.Input.Event.ToString().Contains("Incorrect"))
                        {
                            questionID++;
                        }

                        else if (turn.Input.Event == "Correct")
                        {
                            // question ID should increase
                            questionID++;
                        }
                        foreach (var transition in turn.Transitions)
                        {
                            if (transition.RuleID == "End")
                            {
                                return questionID;
                            }
                        } 
                    }
                    lastTurnID = turn.TurnID;
                }
            }

            return 0;
        }

        // for another format
        public List<String> getInfoOne2(String studentRecord, List<List<int>> quesNum)
        {
            List<String> records = new List<string>();
            String classID = studentRecord.Split(new Char[] { '-' })[0];
            String subjectID = studentRecord.Split(new Char[] { '-' })[1];

            List<List<int>> allQues = new List<List<int>>();

            for (int i = 1; i < 31; i++)
            {
                //2 | 4 | 8 | 10 | 11 | 12 | 13 | 14 | 18 | 26 | 27
                var lessonId = "lesson" + i.ToString();

                List<int> allRecord = new List<int>();

                if (i == 2 || i == 11 || i == 13 || i == 14 || i == 15 || i == 18 || i == 20 || i == 22 || i == 23 || i == 25 || i == 26 || i == 28)
                {
                    allRecord = getPerRecordSpecific(subjectID, lessonId);
                }
                else if (i == 8)
                {
                    allRecord = getPerLesson8(subjectID, lessonId);
                }
                else
                {
                    allRecord = getPerLesson(subjectID, lessonId);
                }

                if (allRecord == null || allRecord.Count < 1)
                {
                    allRecord = new List<int>(new int[] { 0, 0, 0 });
                    allQues.Add(allRecord);
                    continue;
                }
                else
                {
                    allQues.Add(allRecord);
                }
            }

            records = getAllRecords(subjectID, classID, allQues, quesNum);
            return records;
        }

        // get one student's information
        public String getInfoForOne(String studentRecord, List<List<int>> quesNum)
        {
            String classID = studentRecord.Split(new Char[] { '-' })[0];
            String subjectID = studentRecord.Split(new Char[] { '-' })[1];

            String allRecords = subjectID + "\t" + classID;

            List<List<int>> allQues = new List<List<int>>();

            for (int i = 1; i < 31; i++)
            {
                //2 | 4 | 8 | 10 | 11 | 12 | 13 | 14 | 18 | 26 | 27
                var lessonId = "lesson" + i.ToString();

                List<int> allRecord = new List<int>();

                if (i == 2 || i == 11 || i == 13 || i == 14 || i == 15 || i == 18 || i == 20 || i == 22 || i == 23 || i == 25 || i == 26 || i == 28)
                {
                    allRecord = getPerRecordSpecific(subjectID, lessonId);
                }
                else if (i == 8)
                {
                    allRecord = getPerLesson8(subjectID, lessonId);
                }
                else
                {
                    allRecord = getPerLesson(subjectID, lessonId);
                }

                if (allRecord == null || allRecord.Count < 1)
                {
                    allRecord = new List<int>(new int[] { 0, 0, 0 });
                    allQues.Add(allRecord);
                    continue;
                }
                else
                {
                    allQues.Add(allRecord);
                }
            }

            allRecords += getEveryRecords(allQues, quesNum);
            return allRecords;
        }

        // get all the records of one for the second format
        public List<String> getAllRecords(String subjectID, String classID, List<List<int>> quesionNum, List<List<int>> fixedQuesNum)
        {
            List<String> records = new List<string>();
            String perRecord = "";
            double comple = 0.0, correct = 0.0;

            for (int i = 0; i < 30; i++)
            {
                if (quesionNum[i][0] == 0 && quesionNum[i][1] == 0)
                {
                    comple = 0;
                    correct = 0;
                }
                if (quesionNum[i][0] == 1)
                {
                    comple = 1;
                    correct = (float)quesionNum[i][2] / (float)quesionNum[i][1];
                }
                else if (quesionNum[i][1] != 0)
                {
                    comple = getCompletion(fixedQuesNum[1][i], fixedQuesNum[0][i], quesionNum[i][1]);
                    correct = (float)quesionNum[i][2] / (float)quesionNum[i][1];
                }
                else
                {
                    comple = 0;
                    correct = 0;
                }
                perRecord = subjectID + "\t" + classID + "\t" + (i + 1).ToString() + "\t" + comple.ToString() + "\t" + correct.ToString();
                records.Add(perRecord);
            }

            return records;
        }

        public String getEveryRecords(List<List<int>> quesionNum, List<List<int>> fixedQuesNum)
        {
            String perRecord = "";
            double comple = 0.0, correct = 0.0;

            for (int i = 0; i < 30; i++)
            {
                if (quesionNum[i][0] == 0 && quesionNum[i][1] == 0)
                {
                    comple = 0;
                    correct = 0;
                }
                if (quesionNum[i][0] == 1)
                {
                    comple = 1;
                    correct = (float)quesionNum[i][2] / (float)quesionNum[i][1];
                }
                else if (quesionNum[i][1] != 0)
                {
                    comple = getCompletion(fixedQuesNum[1][i], fixedQuesNum[0][i], quesionNum[i][1]);
                    correct = (float)quesionNum[i][2] / (float)quesionNum[i][1];
                }
                else
                {
                    comple = 0;
                    correct = 0;
                }
                perRecord += "\t" + comple.ToString() + "\t" + correct.ToString();
            }

            return perRecord;
        }

        public double getCompletion(int min, int max, int quesNum)
        {
            double comple;

            if (quesNum < min || max == min)
            {
                comple = (float)quesNum / (float)min;
            }
            else
            {
                comple = 1 - ((float)max - (float)quesNum) / (((float)max - (float)min) * (float)min);
            }

            return comple;
        }


        // get all question number
        public List<int> getPerLesson(String studentName, String lessonID)
        {
            var db = new CSALDatabase(DB_URL);
            var oneTurn = db.FindTurns(lessonID, studentName);
            int lastTurnID = 0, questionID = 0, score = 0;

            List<int> quesNum = new List<int>();

            if (oneTurn == null || oneTurn.Count < 1 || oneTurn[0].Turns.Count < 1)
            {
                return null;
            }
            else
            {
                foreach (var turn in oneTurn[0].Turns)
                {
                    // student tried more than 1, reset everything
                    if (turn.TurnID < lastTurnID)
                    {
                        break;
                    }
                    else if (turn.Input.Event == "End")
                    {
                        if (quesNum == null)
                        {
                            quesNum.Add(1);
                            break;
                        }
                        
                    }
                    else
                    {
                        foreach (var transition in turn.Transitions)
                        {

                            // analyze the correct/incorrect of the question
                            if (transition.RuleID == "Correct" || transition.RuleID == "Correct1")
                            {
                                // question ID should increase
                                questionID++;

                                // score & attempt
                                score += 1;
                            }
                            else if (transition.RuleID == "Incorrect" || transition.RuleID == "Incorrect1")
                            {
                                questionID++;
                            }
                            else if (transition.RuleID == "End")
                            {
                                if (quesNum == null)
                                {
                                    quesNum.Add(1);
                                    break;
                                }
                        
                            }
                        }
                    }
                    lastTurnID = turn.TurnID;
                }
            }

            if (quesNum.Count > 0 && quesNum[0] == 1)
            {
                quesNum.Add(questionID);
                quesNum.Add(score);
            }
            else
            {
                quesNum.Add(0);
                quesNum.Add(questionID);
                quesNum.Add(score);
            }

            return quesNum;
        }



        // get record for specific lessons: get score from input.Event
        // lesson 2, 11, 13, 14, 15, 18, 20
        public List<int> getPerRecordSpecific(String studentName, String lessonID)
        {
            var db = new CSALDatabase(DB_URL);
            var oneTurn = db.FindTurns(lessonID, studentName);
            int questionID = 0, lastTurnID = 0, score = 0;

            List<int> quesNum = new List<int>();

            if (oneTurn == null || oneTurn.Count < 1 || oneTurn[0].Turns.Count < 1)
            {
                return null;
            }
            else
            {
                foreach (var turn in oneTurn[0].Turns)
                {
                    // student tried more than 1, reset everything
                    if (turn.TurnID < lastTurnID)
                    {
                        break;
                    }
                    else if (turn.Input.Event == "End")
                    {
                        quesNum.Add(1);
                        break;
                    }
                    else
                    {
                        if (turn.Input.Event.ToString().Contains("Incorrect"))
                        {
                            questionID++;
                        }
                        else if (turn.Input.Event == "Correct")
                        {
                            // question ID should increase
                            questionID++;

                            // score & attempt
                            score += 1;
                        }

                        foreach(var transition in turn.Transitions) {
                            if (transition.RuleID == "End")
                            {
                                quesNum.Add(1);
                                break;
                            }
                        }
                    }

                    lastTurnID = turn.TurnID;
                }
            }

            if (quesNum.Count>0 && quesNum[0] == 1)
            {
                quesNum.Add(questionID);
                quesNum.Add(score);
            }
            else
            {
                quesNum.Add(0);
                quesNum.Add(questionID);
                quesNum.Add(score);
            }

            return quesNum;
        }

        // lesson 8
        public List<int> getPerLesson8(String studentName, String lessonID)
        {
            var db = new CSALDatabase(DB_URL);
            var oneTurn = db.FindTurns(lessonID, studentName);
            int questionID = 0, lastTurnID = 0, score = 0;

            List<int> quesNum = new List<int>();

            if (oneTurn == null || oneTurn.Count < 1 || oneTurn[0].Turns.Count < 1)
            {
                return null;
            }
            else
            {
                foreach (var turn in oneTurn[0].Turns)
                {
                    // student tried more than 1, reset everything
                    if (turn.TurnID < lastTurnID)
                    {
                        break;
                    }
                    else
                    {

                        foreach (var transition in turn.Transitions)
                        {

                            foreach (var action in transition.Actions)
                            {
                                if (action.Act == "AddUserScore" && action.Data.Contains("1"))
                                {
                                    questionID++;
                                    score += 1;
                                    break;
                                }
                                else if (action.Act == "GetMediaFeedback" && action.Data.Contains("SAGoodAnswer"))
                                {
                                    questionID++;
                                    break;
                                }
                            }
                        }

                        lastTurnID = turn.TurnID;
                    }
                }

                if (quesNum == null || quesNum.Count < 1)
                {
                    quesNum.Add(0);
                    quesNum.Add(questionID);
                    quesNum.Add(score);
                }
                if (quesNum[0] == 1)
                {
                    quesNum.Add(questionID);
                    quesNum.Add(score);
                }

                return quesNum;
            }
        }
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
