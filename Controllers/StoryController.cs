using ELearning.Models;
using MySql.Data.MySqlClient;
using NAudio.Lame;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Speech.AudioFormat;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;

namespace ELearning.Controllers
{
    public class StoryController : Controller
    {
        private string usertype = "student";
        string defaultConnection = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        // GET: Story
        public ActionResult Index()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int classID = (int)Session["ClassID"];
            List<StoryModel> list = GetAllStories(classID);

            return View(list);
        }

        public ActionResult Detail(int? id)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var studentid = (int)Session["UserID"];
            StoryModel model = GetStoryByID(id.Value, studentid);

            return View(model);
        }

        public ActionResult Question(int? id)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return HttpNotFound();
            }

            StoryModel course = GetStoriesByID(id.Value);

            QuestionViewModel model = new QuestionViewModel();
            model.Questions = GetQuestionsAndAnswersByStoryID(id.Value);
            model.StoryID = id.Value;
            model.StoryTitle = course.Title;

            return View(model);
        }
         
        [HttpPost]
        public JsonResult QuizResult(QuestionResultViewModel model)
        {
            QuizAssessmentModel resultModel = new QuizAssessmentModel();
            int count = 0;
            int attempt = 1;
            if (model != null)
            {
                attempt += GetStudentMaxAttemptInQuestion(model.StudentID, model.StoryID);

                foreach (var grade in model.QuestionResults)
                {
                    GradeModel gradeModel = new GradeModel();
                    gradeModel.StudentID = model.StudentID;
                    gradeModel.StoryID = model.StoryID;
                    gradeModel.QuestionID = grade.QuestionID;
                    gradeModel.StudentAnswerID = grade.AnswerID;
                    gradeModel.Attempt = attempt;

                    count += InsertStudentAnswer(gradeModel);
                }

                if (count > 0)
                {
                    AnswerComputeModel acModel = new AnswerComputeModel();
                    acModel = GetQuizAssessmentDB(model.StudentID, model.StoryID, attempt);
                    resultModel.QuizAssessmentPercentage = (int)Math.Round(((decimal)acModel.CountCorrectAnswer / acModel.CountTotalQuestion) * 100);

                    if (resultModel.QuizAssessmentPercentage >= 60)
                    {
                        resultModel.QuizAssessmentIcon = "success";
                        resultModel.QuizAssessmentMessage = "Nice job, you passed!";
                    }
                    else
                    {
                        resultModel.QuizAssessmentIcon = "error";
                        resultModel.QuizAssessmentMessage = "Sorry, you didn't pass.";
                    }
                }
                else
                {
                    resultModel.QuizAssessmentPercentage = 0;
                    resultModel.QuizAssessmentIcon = "info";
                    resultModel.QuizAssessmentMessage = "There was an error when retrieving your quiz result.";
                }
            }

            return Json(resultModel);
        }
          
        public JsonResult ViewQuizResult(int storyid)
        { 
            QuizAssessmentModel resultModel = new QuizAssessmentModel();
            AnswerComputeModel model = new AnswerComputeModel();
            var studentid = (int)Session["UserID"];
            model = GetQuizAssessmentDB(studentid, storyid, 1);

            if (model != null)
            {
                resultModel.QuizAssessmentPercentage = (int)Math.Round(((decimal)model.CountCorrectAnswer / model.CountTotalQuestion) * 100);

                if (resultModel.QuizAssessmentPercentage >= 60)
                {
                    resultModel.QuizAssessmentIcon = "success";
                    resultModel.QuizAssessmentMessage = "Nice job, you passed!";
                }
                else
                {
                    resultModel.QuizAssessmentIcon = "error";
                    resultModel.QuizAssessmentMessage = "Sorry, you didn't pass.";
                }
            }
            else
            {
                resultModel.QuizAssessmentPercentage = 0;
                resultModel.QuizAssessmentIcon = "info";
                resultModel.QuizAssessmentMessage = "There was an error when retrieving your quiz result.";
            }


            return Json(resultModel, JsonRequestBehavior.AllowGet);
        } 



        private List<StoryModel> GetAllStories(int classID)
        {
            List<StoryModel> list = new List<StoryModel>();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = @"select c.*, u.name as addedbyname from stories c left join users u on c.addedby = u.id where (c.isdeleted is null or c.isdeleted = 0) and c.classid = @classid order by c.id desc;";
            cmd.Parameters.AddWithValue("@classid", classID);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    StoryModel model = new StoryModel();
                    model.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.Title = rd["title"] != null ? rd["title"].ToString() : "";
                    model.Content = rd["content"] != null ? rd["content"].ToString() : "";
                    model.AddedByName = rd["addedbyname"] != null ? rd["addedbyname"].ToString() : "";
                    model.AddedBy = rd["addedby"] != null && rd["dateadded"].ToString() != "" ? Convert.ToInt32(rd["addedby"].ToString()) : 0;
                    model.DateAdded = rd["dateadded"] != null && rd["dateadded"].ToString() != "" ? Convert.ToDateTime(rd["dateadded"].ToString()) : new DateTime(2000, 1, 1);
                    model.UpdatedBy = rd["updatedby"] != null && rd["updatedby"].ToString() != "" ? Convert.ToInt32(rd["updatedby"].ToString()) : 0;
                    model.DateUpdated = rd["dateupdated"] != null && rd["dateupdated"].ToString() != "" ? Convert.ToDateTime(rd["dateupdated"].ToString()) : new DateTime(2000, 1, 1);
                    model.DeletedBy = rd["deletedby"] != null && rd["deletedby"].ToString() != "" ? Convert.ToInt32(rd["deletedby"].ToString()) : 0;
                    model.DateDeleted = rd["datedeleted"] != null && rd["datedeleted"].ToString() != "" ? Convert.ToDateTime(rd["datedeleted"].ToString()) : new DateTime(2000, 1, 1);
                    model.IsDeleted = rd["isdeleted"] != null && rd["isdeleted"].ToString() != "" ? Convert.ToBoolean(rd["isdeleted"].ToString()) : false;
                    list.Add(model);
                }
                rd.Close();
            } 
            finally
            {
                con.Close();
            }
            return list;
        }

        private StoryModel GetStoryByID(int id, int studentid)
        {
            StoryModel model = new StoryModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "select *, (select COUNT(*) from grades g WHERE g.storyid = s.id and g.studentid = @studentid) as cntgrade from stories s where s.id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@studentid", studentid);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                { 
                    model.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.Title = rd["title"] != null ? rd["title"].ToString() : "";
                    model.Content = rd["content"] != null ? rd["content"].ToString() : ""; 
                    model.AddedBy = rd["addedby"] != null && rd["dateadded"].ToString() != "" ? Convert.ToInt32(rd["addedby"].ToString()) : 0;
                    model.DateAdded = rd["dateadded"] != null && rd["dateadded"].ToString() != "" ? Convert.ToDateTime(rd["dateadded"].ToString()) : new DateTime(2000, 1, 1);
                    model.UpdatedBy = rd["updatedby"] != null && rd["updatedby"].ToString() != "" ? Convert.ToInt32(rd["updatedby"].ToString()) : 0;
                    model.DateUpdated = rd["dateupdated"] != null && rd["dateupdated"].ToString() != "" ? Convert.ToDateTime(rd["dateupdated"].ToString()) : new DateTime(2000, 1, 1);
                    model.DeletedBy = rd["deletedby"] != null && rd["deletedby"].ToString() != "" ? Convert.ToInt32(rd["deletedby"].ToString()) : 0;
                    model.DateDeleted = rd["datedeleted"] != null && rd["datedeleted"].ToString() != "" ? Convert.ToDateTime(rd["datedeleted"].ToString()) : new DateTime(2000, 1, 1);
                    model.IsDeleted = rd["isdeleted"] != null && rd["isdeleted"].ToString() != "" ? Convert.ToBoolean(rd["isdeleted"].ToString()) : false;
                    model.CountGrade = rd["cntgrade"] != null && rd["cntgrade"].ToString() != "" ? Convert.ToInt32(rd["cntgrade"].ToString()) : 0;

                    if (!string.IsNullOrEmpty(model.Content))
                    {
                        // Character limit per page
                        int characterLimit = 1500;

                        // Split the content into pages
                        model.PageContents = CreatePages(model.Content, characterLimit);
                    }
                }
                rd.Close();
            }
            finally
            {
                con.Close();
            }
            return model;
        }

        private List<QuestionModel> GetQuestionsAndAnswersByStoryID(int id)
        {
            List<QuestionModel> list = new List<QuestionModel>();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "select * from questions where (isdeleted is null or isdeleted = 0) and courseid = @id order by id;";
            cmd.Parameters.AddWithValue("@id", id);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    QuestionModel question = new QuestionModel();
                    question.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    question.Question = rd["question"] != null ? rd["question"].ToString() : "";
                    question.AddedBy = rd["addedby"] != null && rd["dateadded"].ToString() != "" ? Convert.ToInt32(rd["addedby"].ToString()) : 0;
                    question.DateAdded = rd["dateadded"] != null && rd["dateadded"].ToString() != "" ? Convert.ToDateTime(rd["dateadded"].ToString()) : new DateTime(2000, 1, 1);
                    question.UpdatedBy = rd["updatedby"] != null && rd["updatedby"].ToString() != "" ? Convert.ToInt32(rd["updatedby"].ToString()) : 0;
                    question.DateUpdated = rd["dateupdated"] != null && rd["dateupdated"].ToString() != "" ? Convert.ToDateTime(rd["dateupdated"].ToString()) : new DateTime(2000, 1, 1);
                    question.Answers = GetAnswersByQuestionID(question.ID);
                    list.Add(question);
                }
                rd.Close();
            }
            finally
            {
                con.Close();
            }
            return list;
        }

        private List<AnswerModel> GetAnswersByQuestionID(int id)
        {
            List<AnswerModel> list = new List<AnswerModel>();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "SELECT * FROM `answers` WHERE questionid = @id order by sequence;";
            cmd.Parameters.AddWithValue("@id", id);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    AnswerModel model = new AnswerModel();
                    model.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.QuestionID = rd["questionid"] != null && rd["questionid"].ToString() != "" ? Convert.ToInt32(rd["questionid"].ToString()) : 0;
                    model.Option = rd["option"] != null ? rd["option"].ToString() : "";
                    model.IsCorrect = rd["iscorrect"] != null && rd["iscorrect"].ToString() != "" ? Convert.ToBoolean(rd["iscorrect"].ToString()) : false;
                    model.Sequence = rd["sequence"] != null && rd["sequence"].ToString() != "" ? Convert.ToInt32(rd["sequence"].ToString()) : 0;
                    list.Add(model);
                }
                rd.Close();
            }
            finally
            {
                con.Close();
            }
            return list;
        }

        private StoryModel GetStoriesByID(int id)
        {
            StoryModel model = new StoryModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "select * from stories where id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    model.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.Title = rd["title"] != null ? rd["title"].ToString() : "";
                    model.Content = rd["content"] != null ? rd["content"].ToString() : "";
                    model.AddedBy = rd["addedby"] != null && rd["dateadded"].ToString() != "" ? Convert.ToInt32(rd["addedby"].ToString()) : 0;
                    model.DateAdded = rd["dateadded"] != null && rd["dateadded"].ToString() != "" ? Convert.ToDateTime(rd["dateadded"].ToString()) : new DateTime(2000, 1, 1);
                    model.UpdatedBy = rd["updatedby"] != null && rd["updatedby"].ToString() != "" ? Convert.ToInt32(rd["updatedby"].ToString()) : 0;
                    model.DateUpdated = rd["dateupdated"] != null && rd["dateupdated"].ToString() != "" ? Convert.ToDateTime(rd["dateupdated"].ToString()) : new DateTime(2000, 1, 1);
                    model.DeletedBy = rd["deletedby"] != null && rd["deletedby"].ToString() != "" ? Convert.ToInt32(rd["deletedby"].ToString()) : 0;
                    model.DateDeleted = rd["datedeleted"] != null && rd["datedeleted"].ToString() != "" ? Convert.ToDateTime(rd["datedeleted"].ToString()) : new DateTime(2000, 1, 1);
                    model.IsDeleted = rd["isdeleted"] != null && rd["isdeleted"].ToString() != "" ? Convert.ToBoolean(rd["isdeleted"].ToString()) : false;
                    rd.Close();
                }
            }
            catch (Exception ex)
            {
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>" + ex.Message + "</p>";
            }
            finally
            {
                con.Close();
            }
            return model;
        }

        private List<PageContent> CreatePages(string content, int charLimit)
        {
            var pages = new List<PageContent>();
            int pageNumber = 1;

            //while (content.Length > 0)
            //{
            //    // Ensure we don't exceed the character limit
            //    string pageContent = content.Substring(0, Math.Min(charLimit, content.Length));
            //    pages.Add(new PageContent
            //    {
            //        PageNumber = pageNumber,
            //        Title = $"Page {pageNumber}",
            //        Content = pageContent
            //    });

            //    // Move to the next page, removing processed content
            //    content = content.Substring(pageContent.Length);
            //    pageNumber++;
            //}




            // Updated Regex pattern to match <p class="fb-page-content"> tag and capture its content
            string pattern = @"<p class=""fb-page-content""[^>]*>(.*?)<\/p>";

            // Find all matches
            MatchCollection matches = Regex.Matches(content, pattern, RegexOptions.Singleline);

            // List to store grouped paragraphs
            List<string> groupedParagraphs = new List<string>();

            // Function to check if content is empty or only contains <br> tags
            bool IsEmptyOrBrTag(string text)
            {
                string trimmedContent = text.Trim();
                return string.IsNullOrEmpty(trimmedContent) || trimmedContent == "<br>" || Regex.IsMatch(trimmedContent, @"^(<br\s*/?>)+$", RegexOptions.IgnoreCase);
            }

            // Loop through and group every two paragraphs
            for (int i = 0; i < matches.Count; i += 2)
            {
                // Extract the inner content of the current and next <p> tags
                string currentContent = matches[i].Groups[1].Value.Trim();
                string nextContent = i + 1 < matches.Count ? matches[i + 1].Groups[1].Value.Trim() : string.Empty;

                // Only include the <p> tag if it contains text other than <br>
                string combinedGroup = string.Empty;
                if (!IsEmptyOrBrTag(currentContent))
                {
                    combinedGroup = matches[i].Value; // Add the first <p> tag
                }
                if (!IsEmptyOrBrTag(nextContent))
                {
                    combinedGroup += matches[i + 1].Value; // Add the second <p> tag if it exists and contains valid content
                }

                // If combinedGroup contains valid content, add it to the list
                if (!string.IsNullOrEmpty(combinedGroup))
                {
                    groupedParagraphs.Add(combinedGroup); // Add the combined group to the list
                }
            }

            // Output each group of paragraphs
            foreach (var group in groupedParagraphs)
            {
                pages.Add(new PageContent
                {
                    PageNumber = pageNumber,
                    Title = $"Page {pageNumber}",
                    Content = group
                });

                // Move to the next page
                pageNumber++;
            }

            return pages;
        }



        private int InsertStudentAnswer(GradeModel model)
        {
            int count = 0;
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "INSERT INTO `grades`(`studentid`, `storyid`, `questionid`, `stud_answerid`, `attempt`, `dateadded`) " +
                                           "VALUES (@studentid, @storyid, @questionid, @stud_answerid, @attempt, @dateadded)";
            cmd.Parameters.AddWithValue("@studentid", model.StudentID);
            cmd.Parameters.AddWithValue("@storyid", model.StoryID);
            cmd.Parameters.AddWithValue("@questionid", model.QuestionID);
            cmd.Parameters.AddWithValue("@stud_answerid", model.StudentAnswerID);
            cmd.Parameters.AddWithValue("@attempt", model.Attempt);
            cmd.Parameters.AddWithValue("@dateadded", DateTime.Now);

            try
            {
                con.Open();
                count = cmd.ExecuteNonQuery();
            } 
            finally
            {
                con.Close();
            }
            return count;
        }

        private int GetStudentMaxAttemptInQuestion(int studentid, int storyid)
        {
            int attempt = 0;
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "SELECT MAX(attempt) attempt FROM grades where studentid = @studentid and storyid = @storyid";
            cmd.Parameters.AddWithValue("@studentid", studentid);
            cmd.Parameters.AddWithValue("@storyid", storyid);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                { 
                    attempt = rd["attempt"] != null && rd["attempt"].ToString() != "" ? Convert.ToInt32(rd["attempt"].ToString()) : 0; 
                }
            }
            finally
            {
                con.Close();
            }
            return attempt;
        }

        private AnswerComputeModel GetQuizAssessmentDB(int studentid, int storyid, int attempt)
        { 
            AnswerComputeModel model = new AnswerComputeModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = @"SELECT SUM(case when a.iscorrect is null or a.iscorrect = 0 then 0 else 1 end) as CntCorrect, (select COUNT(*) from `questions` gg where (gg.isdeleted is null or gg.isdeleted = 0) and gg.courseid = g.storyid) as CntTotal 
                                FROM `grades` g INNER JOIN `answers` a on g.stud_answerid = a.id 
                                WHERE g.attempt = @attempt and g.studentid = @studentid and g.storyid = @storyid;";
            cmd.Parameters.AddWithValue("@studentid", studentid);
            cmd.Parameters.AddWithValue("@storyid", storyid);
            cmd.Parameters.AddWithValue("@attempt", attempt); 

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    model.CountCorrectAnswer = rd["CntCorrect"] != null && rd["CntCorrect"].ToString() != "" ? Convert.ToInt32(rd["CntCorrect"].ToString()) : 0;
                    model.CountTotalQuestion = rd["CntTotal"] != null && rd["CntTotal"].ToString() != "" ? Convert.ToInt32(rd["CntTotal"].ToString()) : 0; 
                }
            }
            finally
            {
                con.Close();
            }
            return model;
        }

        #region Read Story Content
        public FileResult TextToMp3(string text)
        {
            //Primary memory stream for storing mp3 audio
            var mp3Stream = new MemoryStream();
            //Speech format
            var speechAudioFormatConfig = new SpeechAudioFormatInfo
            (samplesPerSecond: 8000, bitsPerSample: AudioBitsPerSample.Sixteen,
            channel: AudioChannel.Stereo);
            //Naudio's wave format used for mp3 conversion. 
            //Mirror configuration of speech config.
            var waveFormat = new WaveFormat(speechAudioFormatConfig.SamplesPerSecond,
            speechAudioFormatConfig.BitsPerSample, speechAudioFormatConfig.ChannelCount);
            try
            {
                //Build a voice prompt to have the voice talk slower 
                //and with an emphasis on words
                var prompt = new PromptBuilder
                { Culture = CultureInfo.CreateSpecificCulture("en-US") };
                prompt.StartVoice(prompt.Culture);
                prompt.StartSentence();
                prompt.StartStyle(new PromptStyle()
                { Emphasis = PromptEmphasis.Reduced, Rate = PromptRate.Slow });
                prompt.AppendText(text);
                prompt.EndStyle();
                prompt.EndSentence();
                prompt.EndVoice();

                //Wav stream output of converted text to speech
                using (var synthWavMs = new MemoryStream())
                {
                    //Spin off a new thread that's safe for an ASP.NET application pool.
                    var resetEvent = new ManualResetEvent(false);
                    ThreadPool.QueueUserWorkItem(arg =>
                    {
                        try
                        {
                            //initialize a voice with standard settings
                            var siteSpeechSynth = new SpeechSynthesizer();
                            //Set memory stream and audio format to speech synthesizer
                            siteSpeechSynth.SetOutputToAudioStream
                                (synthWavMs, speechAudioFormatConfig);
                            //build a speech prompt
                            siteSpeechSynth.Speak(prompt);
                        }
                        catch (Exception ex)
                        {
                            //This is here to diagnostic any issues with the conversion process. 
                            //It can be removed after testing.
                            Response.AddHeader
                            ("EXCEPTION", ex.GetBaseException().ToString());
                        }
                        finally
                        {
                            resetEvent.Set();//end of thread
                        }
                    });
                    //Wait until thread catches up with us
                    WaitHandle.WaitAll(new WaitHandle[] { resetEvent });
                    //Estimated bitrate
                    var bitRate = (speechAudioFormatConfig.AverageBytesPerSecond * 8);
                    //Set at starting position
                    synthWavMs.Position = 0;
                    //Be sure to have a bin folder with lame dll files in there. 
                    //They also need to be loaded on application start up via Global.asax file
                    using (var mp3FileWriter = new LameMP3FileWriter
                    (outStream: mp3Stream, format: waveFormat, bitRate: bitRate))
                        synthWavMs.CopyTo(mp3FileWriter);
                }
            }
            catch (Exception ex)
            {
                Response.AddHeader("EXCEPTION", ex.GetBaseException().ToString());
            }
            finally
            {
                //Set no cache on this file
                Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetNoStore();
                //required for chrome and safari
                Response.AppendHeader("Accept-Ranges", "bytes");
                //Write the byte length of mp3 to the client
                Response.AddHeader("Content-Length",
                    mp3Stream.Length.ToString(CultureInfo.InvariantCulture));
            }
            //return the converted wav to mp3 stream to a byte array for a file download
            return File(mp3Stream.ToArray(), "audio/mp3");
        }

        public ActionResult PlayTextArea(string text)
        { 
            return TextToMp3(text);
        }
        #endregion Read Story Content
    }
}