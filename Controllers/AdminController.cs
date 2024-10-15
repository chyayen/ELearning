using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using ELearning.Models;
using MySql.Data.MySqlClient;
using PagedList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data; 
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ELearning.Controllers
{
    public class AdminController : Controller
    {
        private string usertype = "teacher";
        string defaultConnection = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
         
        public ActionResult Index()
        {
            if (Session["UserName"] != null)
            {
                if(Session["UserType"].ToString() == "student")
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return View();
                }
            }
            else
            {
                return RedirectToAction("Login");
            }
        }


        #region Login
        public ActionResult Login()
        {
            if (Session["UserName"] != null)
            {
                return RedirectToAction("Index");
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            var userdb = GetUserLogin(model.UserName, model.Password);

            if (userdb != null && !string.IsNullOrEmpty(userdb.UserName))
            {
                Session.Timeout = 3600;
                Session["UserID"] = userdb.UserID;
                Session["UserName"] = userdb.UserName;
                Session["FullName"] = userdb.FullName;
                Session["Email"] = userdb.Email;
                Session["UserType"] = userdb.UserType;
                Session["DefaultImageName"] = userdb.DefaultImageName;
                Session["CountNotVeriedStudents"] = userdb.CountNotVeriedStudents;
                return RedirectToAction("Index"); 
            }
            else
            {
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>Incorrect username or password.</p>";
            }

            return View(model);
        }

        [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
        public ActionResult Logout(LoginViewModel model)
        {
            Session.Abandon(); // it will clear the session at the end of request
            return RedirectToAction("Login");
        }

        public ActionResult AccountSettings(int? id)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login");
            }

            if (id == null)
            {
                return HttpNotFound();
            }

            return View();
        }

        private LoginViewModel GetUserLogin(string username, string password)
        {
            LoginViewModel model = new LoginViewModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "CALL `get_userlogin`(@p0, @p1, @p2)";
            cmd.Parameters.AddWithValue("@p0", username);
            cmd.Parameters.AddWithValue("@p1", password);
            cmd.Parameters.AddWithValue("@p2", usertype);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    model.UserID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.UserName = rd["username"] != null ? rd["username"].ToString() : "";
                    model.Password = rd["password"] != null ? rd["password"].ToString() : "";
                    model.IsVerified = rd["isverified"] != null ? (rd["isverified"].ToString() == "1") : false;
                    model.FullName = rd["name"] != null ? rd["name"].ToString() : "";
                    model.Email = rd["email"] != null ? rd["email"].ToString() : "";
                    model.UserType = rd["usertype"] != null ? rd["usertype"].ToString() : "";
                    model.DefaultImageName = rd["defaultimagename"] != null ? rd["defaultimagename"].ToString() : ""; 
                    model.CountNotVeriedStudents = rd["cntNotVerify"] != null && rd["cntNotVerify"].ToString() != "" ? Convert.ToInt32(rd["cntNotVerify"].ToString()) : 0;
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


        #endregion Login


        #region Register
        public ActionResult Register()
        {
            RegisterViewModel model = new RegisterViewModel();
            model.UserType = usertype;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (UserInsertDB(model) > 0)
            {
                return RedirectToAction("Login");
            }

            return View(model);
        }

        private int UserInsertDB(RegisterViewModel model)
        {
            int count = 0;
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "CALL `user_insert`(@p0, @p1, @p2, @p3, @p4, @p5)";
            cmd.Parameters.AddWithValue("@p0", model.UserName);
            cmd.Parameters.AddWithValue("@p1", model.FullName);
            cmd.Parameters.AddWithValue("@p2", model.Email);
            cmd.Parameters.AddWithValue("@p3", model.Password);
            cmd.Parameters.AddWithValue("@p4", usertype);
            cmd.Parameters.AddWithValue("@p5", false);

            try
            {
                con.Open();
                count = cmd.ExecuteNonQuery();
                if (count > 0)
                {
                    ViewData["AlertMessage"] = "<p class='alert alert-success'>Your registration is successful.</p>";
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
            return count;

        }
        #endregion Register

        #region Classes
        public ActionResult ManageClasses(int? page)
        {
            if (Session["UserName"] != null)
            {
                if (Session["UserType"].ToString() == "student")
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }

            ClassViewModel model = new ClassViewModel();
            int pageSize = 25;
            int pageNumber = (page ?? 1);

            List<ClassModel> list = GetAllClasses();
            model.Classes = list.ToPagedList(pageNumber, pageSize);

            return View(model);
        }  
        [HttpPost] 
        public JsonResult AddClass(ClassModel model, string action)
        { 
            ResultModel resultModel = new ResultModel();
            try
            { 
                resultModel = SaveClassDB(model, action); 
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message; 
            }
            return Json(resultModel);
        }
         
        [HttpPost]
        public JsonResult SaveClass(int classid, string edit_text, string action)
        {
            ResultModel resultModel = new ResultModel();
            try
            {
                if(action == "update")
                {
                    ClassModel model = new ClassModel();
                    model.ID = classid;
                    model.Name = edit_text;
                    resultModel = SaveClassDB(model, action);
                }
                else
                {
                    ClassModel model = new ClassModel();
                    model.ID = classid; 
                    resultModel = SaveClassDB(model, action);
                }
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message;
                return Json(resultModel);
            }
            return Json(resultModel);
        }


        private List<ClassModel> GetAllClasses()
        {
            List<ClassModel> list = new List<ClassModel>();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "select * from classes";

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    ClassModel model = new ClassModel();
                    model.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.Name = rd["name"] != null ? rd["name"].ToString() : ""; 
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
        private ResultModel SaveClassDB(ClassModel model, string action)
        {
            int count = 0;
            string resultActionMessage = "save";
            ResultModel resultModel = new ResultModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();

            if (action == "update")
            {
                cmd.CommandText = @"UPDATE `classes` SET `name` = @name WHERE `id` = @id";
                cmd.Parameters.AddWithValue("@id", model.ID);
                cmd.Parameters.AddWithValue("@name", model.Name); 
            }
            else if (action == "delete")
            {
                resultActionMessage = action;
                cmd.CommandText = "DELETE FROM `classes` WHERE `id` = @id";
                cmd.Parameters.AddWithValue("@id", model.ID);
            }
            else
            {
                cmd.CommandText = @"INSERT INTO `classes`(`name`) VALUES (@name);";
                cmd.Parameters.AddWithValue("@name", model.Name);
            }

            try
            {
                con.Open();
                count = cmd.ExecuteNonQuery();
                resultModel.success = count > 0;
                resultModel.message = count > 0 ? $"Class successfully {resultActionMessage}d." : $"Failed to {resultActionMessage} class. Please check your data.";
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message + "<br>Inner Exception: " + ex.InnerException.Message;
            }
            finally
            {
                con.Close();
            }
            return resultModel;
        }
        #endregion Classes



        #region Students
        public ActionResult ManageStudents(string keywords, int? status, int? classid, int? page)
        {
            if (Session["UserName"] != null)
            {
                if (Session["UserType"].ToString() == "student")
                {
                    return RedirectToAction("Index", "Home");
                }  
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }

            StudentViewModel model = new StudentViewModel();
            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
            int pageSize = 25;
            int pageNumber = (page ?? 1);
             
            List<StudentModel> list = GetAllStudents();

            if (list != null && list.Count > 0)
            {
                if (!string.IsNullOrEmpty(keywords))
                {
                    list = list.Where(l => l.Name.Contains(keywords) || l.Email.Contains(keywords) || l.UserName.Contains(keywords)).ToList();
                }

                if(status.HasValue)
                {
                    var stats = (status.Value == 1);
                    list = list.Where(l => l.IsVerified == stats).ToList();
                }

                if(classid.HasValue)
                {
                    list = list.Where(l => l.ClassID == classid.Value).ToList();
                }
            }

            model.Students = list.ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        public ActionResult AddStudent()
        {
            if (Session["UserName"] != null)
            {
                if (Session["UserType"].ToString() == "student")
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }

            StudentModel model = new StudentModel();
            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddStudent(StudentModel model)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login");
            }

            ResultModel resultModel = new ResultModel();
            try
            {
                if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                {
                    string filetype = System.IO.Path.GetExtension(model.ImageFile.FileName);
                    if (filetype.Contains(".jpg") || filetype.Contains(".jpeg") || filetype.Contains(".png"))
                    {
                        model.DefaultImageName = model.UserName + filetype; 
                    }
                }

                model.UpdatedBy = (int)Session["UserID"];
                resultModel = SaveStudentDB(model, "add");

                if (resultModel.success)
                {
                    if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                    {
                        string path = Server.MapPath("~/Images/Users/" + model.DefaultImageName);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);

                        model.ImageFile.SaveAs(path);
                    }

                    ViewData["AlertMessage"] = $"<p class='alert alert-{(resultModel.success ? "success" : "danger")}'>" + resultModel.message + "</p>";
                    return RedirectToAction("ManageStudents");
                }
                else
                {
                    ViewData["AlertMessage"] = $"<p class='alert alert-{(resultModel.success ? "success" : "danger")}'>" + resultModel.message + "</p>";
                }
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message;
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>" + ex.Message + "</p>";
            }

            StudentModel returnModel = new StudentModel();
            returnModel.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() }); 
            return View(returnModel);
        }

        public ActionResult EditStudent(int? id)
        {
            if (Session["UserName"] != null)
            {
                if (Session["UserType"].ToString() == "student")
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }

            StudentModel model = new StudentModel();
            model = GetStudentById(id.Value);
            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStudent(StudentModel model)
        {
            ResultModel resultModel = new ResultModel(); 
            try
            {
                if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                {
                    string filetype = System.IO.Path.GetExtension(model.ImageFile.FileName);
                    if (filetype.Contains(".jpg") || filetype.Contains(".jpeg") || filetype.Contains(".png"))
                    {
                        model.DefaultImageName = model.UserName + filetype;
                    }
                }
                 
                model.UpdatedBy = (int)Session["UserID"];
                resultModel = SaveStudentDB(model, "update");

                if (resultModel.success)
                {
                    if (model.ImageFile != null && model.ImageFile.ContentLength > 0)
                    {
                        string path = Server.MapPath("~/Images/Users/" + model.DefaultImageName);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);

                        model.ImageFile.SaveAs(path);
                    }

                    ViewData["AlertMessage"] = $"<p class='alert alert-{(resultModel.success ? "success" : "danger")}'>" + resultModel.message + "</p>";
                    return RedirectToAction("ManageStudents");
                }
                else
                { 
                    model = GetStudentById(model.ID);
                    ViewData["AlertMessage"] = $"<p class='alert alert-{(resultModel.success ? "success" : "danger")}'>" + resultModel.message + "</p>";
                }
            }
            catch (Exception ex)
            {
                model = GetStudentById(model.ID);
                resultModel.success = false;
                resultModel.message = ex.Message;
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>" + resultModel.message + "</p>";
            }

            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
            return View(model);
        }

        [HttpPost]
        public JsonResult DeleteStudent(StudentModel model, string action)
        {
            ResultModel resultModel = new ResultModel();
            resultModel = SaveStudentDB(model, action);
            return Json(resultModel);
        }


        [HttpPost]
        public FileResult ExportStudentList()
        { 
            DataTable dt = new DataTable("Students");
            dt.Columns.AddRange(new DataColumn[3] { new DataColumn("User Name"),
                                            new DataColumn("Full Name"),
                                            new DataColumn("Email")});

            List<StudentModel> list = GetAllStudents();

            foreach (var stud in list)
            {
                dt.Rows.Add(stud.UserName, stud.Name, stud.Email);
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Students.xlsx");
                }
            }
        }


        public ActionResult TrackStudentProgress(int? id)
        {
            if (Session["UserName"] != null)
            {
                if (Session["UserType"].ToString() == "student")
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login", "Admin");
            }

            List<StudentTrackingModel> model = GetStudentProgress(id.Value);


            return View(model);
        }

        private List<StudentModel> GetAllStudents()
        {
            List<StudentModel> list = new List<StudentModel>();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = @"select u.*, sc.classid, c.name as classname from users u
                                    left join studentclasses sc on sc.studentid = u.id
                                    left join classes c on c.id = sc.classid
                                    where u.usertype = 'student'"; 

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    StudentModel model = new StudentModel();
                    model.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.UserName = rd["username"] != null ? rd["username"].ToString() : "";
                    model.Name = rd["name"] != null ? rd["name"].ToString() : "";
                    model.Email = rd["email"] != null ? rd["email"].ToString() : "";
                    model.IsVerified = rd["isverified"] != null && rd["isverified"].ToString() != "" ? Convert.ToBoolean(rd["isverified"].ToString()) : false;
                    model.DefaultImageName = rd["defaultimagename"] != null ? rd["defaultimagename"].ToString() : "";
                    model.ClassID = rd["classid"] != null && rd["classid"].ToString() != "" ? Convert.ToInt32(rd["classid"].ToString()) : 0;
                    model.ClassName = rd["classname"] != null ? rd["classname"].ToString() : "";
                    model.IsActive = rd["isactive"] != null && rd["isactive"].ToString() != "" ? Convert.ToBoolean(rd["isactive"].ToString()) : false;
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
        private StudentModel GetStudentById(int id)
        {
            StudentModel model = new StudentModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "select u.*, sc.classid from users u left join studentclasses sc on sc.studentid = u.id where u.id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                { 
                    model.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    model.UserName = rd["username"] != null ? rd["username"].ToString() : "";
                    model.Name = rd["name"] != null ? rd["name"].ToString() : "";
                    model.Email = rd["email"] != null ? rd["email"].ToString() : "";
                    model.IsVerified = rd["isverified"] != null && rd["isverified"].ToString() != "" ? Convert.ToBoolean(rd["isverified"].ToString()) : false;
                    model.DefaultImageName = rd["defaultimagename"] != null ? rd["defaultimagename"].ToString() : "";
                    model.Password = rd["password"] != null ? rd["password"].ToString() : ""; 
                    model.ClassID = rd["classid"] != null && rd["classid"].ToString() != "" ? Convert.ToInt32(rd["classid"].ToString()) : 0;
                    model.IsActive = rd["isactive"] != null && rd["isactive"].ToString() != "" ? Convert.ToBoolean(rd["isactive"].ToString()) : false;
                    model.IsVerified = rd["isverified"] != null && rd["isverified"].ToString() != "" ? Convert.ToBoolean(rd["isverified"].ToString()) : false;
                }
                rd.Close();
            }
            finally
            {
                con.Close();
            }
            return model;
        }
        private ResultModel SaveStudentDB(StudentModel model, string action)
        {
            int count = 0;
            string resultActionMessage = "save";
            ResultModel resultModel = new ResultModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();

            if (action == "update")
            {
                cmd.CommandText = @"UPDATE `users` SET `username`=@username,`name`=@name,`email`=@email,`password`=(case when @password is null or @password = '' then password else @password end)
                                        , `defaultimagename`=(case when @defaultimagename = '' then defaultimagename else @defaultimagename end), `isactive` = @isactive, `isverified` = @isverified 
                                    WHERE `id` = @id;
 
                                    IF (SELECT COUNT(*) FROM `studentclasses` WHERE `studentid` = @id) > 0 THEN 
                                        UPDATE `studentclasses` 
                                        SET `classid` = @classid 
                                        WHERE `studentid` = @id;
                                    ELSE 
                                        INSERT INTO `studentclasses` (`studentid`, `classid`)
                                        VALUES (@id, @classid);
                                    END IF;";
                cmd.Parameters.AddWithValue("@id", model.ID);
                cmd.Parameters.AddWithValue("@username", model.UserName);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@email", model.Email);
                cmd.Parameters.AddWithValue("@password", model.Password);
                cmd.Parameters.AddWithValue("@defaultimagename", model.DefaultImageName);
                cmd.Parameters.AddWithValue("@classid", model.ClassID);
                cmd.Parameters.AddWithValue("@isactive", model.IsActive);
                cmd.Parameters.AddWithValue("@isverified", model.IsVerified);
            }
            else if (action == "delete")
            {
                resultActionMessage = action;
                cmd.CommandText = @"DELETE FROM `users` WHERE `id` = @id;
                                    DELETE FROM studentclasses where studentid = @id;";
                cmd.Parameters.AddWithValue("@id", model.ID);
            }
            else
            {
                cmd.CommandText = @"INSERT INTO `users`(`username`, `name`, `email`, `password`, `isverified`, `usertype`, `defaultimagename`, `isactive`) 
                                    VALUES (@username,@name,@email,@password,1,@usertype,@defaultimagename,1);
                                    
                                    IF @usertype = 'student' THEN   
                                        INSERT INTO `studentclasses`(`studentid`, `classid`)
                                        VALUES (LAST_INSERT_ID(), @classid);
                                    END IF;
                                    ";
                cmd.Parameters.AddWithValue("@username", model.UserName);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@email", model.Email);
                cmd.Parameters.AddWithValue("@password", "1234");
                cmd.Parameters.AddWithValue("@usertype", "student");
                cmd.Parameters.AddWithValue("@defaultimagename", model.DefaultImageName);
                cmd.Parameters.AddWithValue("@teacherid", model.UpdatedBy);
                cmd.Parameters.AddWithValue("@classid", model.ClassID); 
            }

            try
            {
                con.Open();
                count = cmd.ExecuteNonQuery();
                resultModel.success = count > 0;
                resultModel.message = count > 0 ? $"Student successfully {resultActionMessage}d." : $"Failed to {resultActionMessage} student. Please check your data.";
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message + ( ex.InnerException != null ? "<br>Inner Exception: " + ex.InnerException.Message : "");
            }
            finally
            {
                con.Close();
            }
            return resultModel;
        }

        private List<StudentTrackingModel> GetStudentProgress(int studentid)
        {
            List<StudentTrackingModel> list = new List<StudentTrackingModel>();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = @"SELECT stud.StoryTitle, stud.StudentName
	                                , CAST(((CASE WHEN stud.CntTotal = 0 THEN 0 ELSE ((CAST(stud.CntCorrect AS DECIMAL(10,2)))/(CAST(stud.CntTotal AS DECIMAL(10,2)))) END) * 100.0) AS DECIMAL(10,2)) as ResultPercentage 
                                FROM (
                                    SELECT s.title as StoryTitle, u.name as StudentName
    	                                , SUM(case when a.iscorrect is null or a.iscorrect = 0 then 0 else 1 end) as CntCorrect
                                        , (select COUNT(*) from `questions` gg where (gg.isdeleted is null or gg.isdeleted = 0) and gg.courseid = g.storyid) as CntTotal 
                                    FROM `grades` g INNER JOIN `answers` a on g.stud_answerid = a.id 
                                    left join stories s on s.id = g.storyid
                                    left join users u on u.id = g.studentid
                                    WHERE g.studentid = @studentid
                                    GROUP BY s.title, u.name
                                ) stud;";
            cmd.Parameters.AddWithValue("@studentid", studentid);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    StudentTrackingModel model = new StudentTrackingModel(); 
                    model.StoryTitle = rd["StoryTitle"] != null ? rd["StoryTitle"].ToString() : "";
                    model.StudentName = rd["StudentName"] != null ? rd["StudentName"].ToString() : "";
                    model.ResultPercentage = rd["ResultPercentage"] != null && rd["ResultPercentage"].ToString() != "" ? Convert.ToDecimal(rd["ResultPercentage"].ToString()) : 0; 
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

        #endregion Students


        #region Stories
        public ActionResult ManageStories(int? page)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login");
            }

            StoryViewModel model = new StoryViewModel();
            int pageSize = 25;
            int pageNumber = (page ?? 1);

            int teacherid = Convert.ToInt32(Session["UserID"].ToString());
            List<StoryModel> list = GetStoriesByTeacherID(teacherid);
            model.Stories = list.ToPagedList(pageNumber, pageSize);

            return View(model);
        }

        public ActionResult AddStory()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login");
            }

            StoryModel model = new StoryModel();
            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
            return View(model);
        }

        [HttpPost] 
        public JsonResult SaveStory(StoryModel model, string action)
        { 
            ResultModel resultModel = new ResultModel();
            try
            {
                model.UpdatedBy = (int)Session["UserID"];
                resultModel = SaveStoryDB(model, action);
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message;
                return Json(resultModel);
            }
            return Json(resultModel);
        }

        public ActionResult EditStory(int? id)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login");
            }

            if(id == null)
            {
                return HttpNotFound();
            }

            StoryModel model = new StoryModel();
            model = GetStoriesByID(id.Value);
            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });


            return View(model);
        }

        [HttpPost]
        public ActionResult UploadStoryImages(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                // Get the file name and save path
                var fileName = System.IO.Path.GetFileName(file.FileName);
                var fileNameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(file.FileName);
                var fileExt = System.IO.Path.GetExtension(file.FileName);
                var newFileName = string.Concat(fileNameWithoutExt, DateTime.Now.Ticks.ToString(), fileExt);
                var filePath = System.IO.Path.Combine(Server.MapPath("~/Uploads/Stories"), newFileName);

                // Save the file to the server
                file.SaveAs(filePath);

                // Return the file URL to be inserted into the Summernote editor
                return Json(Url.Content("~/Uploads/Stories/" + newFileName));
            }
            return Json(null);
        }

        public ActionResult UploadStory()
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login");
            }

            StoryFileUploadViewModel model = new StoryFileUploadViewModel();
            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
            return View(model);
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult UploadStory(StoryFileUploadViewModel model)
        //{
        //    ResultModel resultModel = new ResultModel();
        //    if (model.ExcelFile != null && model.ExcelFile.ContentLength > 0)
        //    {
        //        var file = model.ExcelFile; 

        //        using (var workbook = new XLWorkbook(file.InputStream))
        //        {
        //            var worksheet = workbook.Worksheets.Worksheet(1); // Get the first worksheet

        //            // Assuming the first row contains column headers
        //            for (int row = 2; row <= worksheet.RowsUsed().Count(); row++)
        //            {
        //                var title = worksheet.Cell(row, 1).GetValue<string>();
        //                var content = worksheet.Cell(row, 2).GetValue<string>();

        //                StoryModel storymodel = new StoryModel();
        //                storymodel.UpdatedBy = (int)Session["UserID"];
        //                storymodel.Title = title;
        //                storymodel.Content = content;
        //                resultModel = SaveStoryDB(storymodel, "add");
        //            }
        //        }

        //        // Process or save the stories as needed
        //        // e.g., save to the database or process the list

        //        return RedirectToAction("ManageStories"); // Redirect to another action or view
        //    }

        //    return View(model);
        //} 

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult UploadStory(StoryFileUploadViewModel model)
        //{
        //    ResultModel resultModel = new ResultModel();
        //    if (model.File != null && model.File.ContentLength > 0)
        //    {
        //        // Ensure the uploaded file is a Word document
        //        var extension = Path.GetExtension(model.File.FileName);
        //        if (extension == ".docx")
        //        {
        //            // Read the Word document
        //            using (var stream = model.File.InputStream)
        //            {
        //                using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(stream, false))
        //                {
        //                    // Get all paragraphs from the document
        //                    var paragraphs = wordDoc.MainDocumentPart.Document.Body.Elements<Paragraph>()
        //                                            .Select(p => p.InnerText)
        //                                            .Where(text => !string.IsNullOrWhiteSpace(text))
        //                                            .ToList();

        //                    // Set the title as the first paragraph and the story content as the rest
        //                    if (paragraphs.Count > 0)
        //                    { 
        //                        StoryModel storymodel = new StoryModel();
        //                        storymodel.UpdatedBy = (int)Session["UserID"];
        //                        storymodel.Title = paragraphs[0];
        //                        storymodel.Content = string.Join(Environment.NewLine, paragraphs.Skip(1));
        //                        storymodel.ClassID = model.ClassID;
        //                        resultModel = SaveStoryDB(storymodel, "add");
        //                    } 
        //                }
        //            }

        //            return RedirectToAction("ManageStories"); // Redirect to another action or view
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("", "Please upload a valid .docx file.");
        //            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
        //        }
        //    }
        //    else
        //    {
        //        ModelState.AddModelError("", "No file was uploaded.");
        //    }

        //    return View(model);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadStory(StoryFileUploadViewModel model)
        {
            ResultModel resultModel = new ResultModel();
            if (model.File != null && model.File.ContentLength > 0)
            {
                // Ensure the uploaded file is a Word document
                var extension = System.IO.Path.GetExtension(model.File.FileName);
                if (extension == ".docx")
                {
                    // Save the uploaded document
                    string filePath = System.IO.Path.Combine(Server.MapPath("~/Uploads"), System.IO.Path.GetFileName(model.File.FileName));
                    model.File.SaveAs(filePath);

                    // Lists to store extracted content
                    List<string> flipbookContent = new List<string>();
                    string title = string.Empty; // Initialize title variable

                    // Open the Word document
                    using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
                    {
                        // Extract paragraphs
                        var body = doc.MainDocumentPart.Document.Body;
                        bool isFirstParagraph = true; // Flag to check if it's the first paragraph

                        foreach (var element in body.Elements())
                        {
                            if (element is DocumentFormat.OpenXml.Wordprocessing.Paragraph para)
                            {
                                string text = para.InnerText.Trim();
                                if (isFirstParagraph)
                                {
                                    // Set the first paragraph as the title and skip adding it to content
                                    title = text;
                                    isFirstParagraph = false; // Set the flag to false after first paragraph
                                }
                                else if (!string.IsNullOrEmpty(text))
                                {
                                    // Add paragraph wrapped in fb-page-content class
                                    flipbookContent.Add($"<p class='fb-page-content'>{text}</p>");
                                }
                            }

                            // Check for Drawing elements within paragraphs to extract images
                            foreach (var drawing in element.Descendants<Drawing>())
                            {
                                // Process the drawing element to extract the image
                                var blip = drawing.Descendants<Blip>().FirstOrDefault();
                                if (blip != null)
                                {
                                    // Get the image part
                                    var imagePart = (ImagePart)doc.MainDocumentPart.GetPartById(blip.Embed.Value);
                                    // Generate a unique filename for the image
                                    string imageFileName = Guid.NewGuid().ToString() + ".png";
                                    string imagePath = System.IO.Path.Combine(Server.MapPath("~/Uploads/Stories"), imageFileName);

                                    // Save the image
                                    using (var stream = new FileStream(imagePath, FileMode.Create))
                                    {
                                        imagePart.GetStream().CopyTo(stream);
                                    }

                                    // Add image HTML wrapped in fb-page-content class
                                    string imageUrl = Url.Content("~/Uploads/Stories/" + imageFileName);
                                    flipbookContent.Add($"<p class='fb-page-content'><img src='{imageUrl}' alt='Image' /></p>");
                                }
                            }
                        }
                    }

                    //Set the title as the first paragraph and the story content as the rest
                    if (flipbookContent.Count > 0)
                    {
                        StoryModel storymodel = new StoryModel();
                        storymodel.UpdatedBy = (int)Session["UserID"];
                        storymodel.Title = title; // Pass the title to the view 
                        storymodel.Content = string.Join("", flipbookContent); // Combine paragraphs and images for display
                        storymodel.ClassID = model.ClassID;
                        resultModel = SaveStoryDB(storymodel, "add");

                        if(resultModel.success)
                        {
                            return RedirectToAction("ManageStories"); // Redirect to story list
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("", "Content is not compatible.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Please upload a valid .docx file."); 
                }
            }
            else
            {
                ModelState.AddModelError("", "No file was uploaded.");
            }
            model.ClassList = GetAllClasses().AsEnumerable().Select(c => new SelectListItem() { Text = c.Name, Value = c.ID.ToString() });
            return View(model);
        }

        public ActionResult DownloadStoryFormat()
        {
            // Define the folder where your files are stored 
            var filePath = Server.MapPath("~/Uploads/StoryFormat.xlsx");

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return HttpNotFound(); // Return a 404 error if the file is not found
            }

            // Get the file's content type
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileBytes = System.IO.File.ReadAllBytes(filePath);

            // Return the file as a download
            return File(fileBytes, contentType, "StoryFormat.xlsx");
        }

        public ActionResult EvaluateStudentSubmissions(int? id)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login");
            }

            if (id == null)
            {
                return HttpNotFound();
            }

            List<StudentSubmissionModel> model = GetStudentSubmissions(id.Value);

            return View(model);
        }



        private List<StoryModel> GetStoriesByTeacherID(int id)
        {
            List<StoryModel> list = new List<StoryModel>();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = @"select *, c.name as classname from stories s 
                                left join classes c on s.classid = c.id 
                                where (s.isdeleted is null or s.isdeleted = 0) and s.addedby = @addedby order by s.id desc";
            cmd.Parameters.AddWithValue("@addedby", id); 

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
                    model.ClassID = rd["classid"] != null && rd["classid"].ToString() != "" ? Convert.ToInt32(rd["classid"].ToString()) : 0;
                    model.ClassName = rd["classname"] != null ? rd["classname"].ToString() : "";
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
            catch (Exception ex)
            {
                ViewData["AlertMessage"] = "<p class='alert alert-danger'>" + ex.Message + "</p>";
            }
            finally
            {
                con.Close();
            }
            return list;
        }

        private ResultModel SaveStoryDB(StoryModel model, string action)
        {
            int count = 0;
            string resultActionMessage = "save";
            ResultModel resultModel = new ResultModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();

            if (action == "update")
            {
                cmd.CommandText = "UPDATE `stories` SET `title`=@title,`content`=@content,`classid`=@classid,`updatedby`=@updatedby,`dateupdated`=@dateupdated WHERE `id` = @id";
                cmd.Parameters.AddWithValue("@id", model.ID);
                cmd.Parameters.AddWithValue("@title", model.Title);
                cmd.Parameters.AddWithValue("@content", model.Content);
                cmd.Parameters.AddWithValue("@classid", model.ClassID);
                cmd.Parameters.AddWithValue("@updatedby", model.UpdatedBy); 
                cmd.Parameters.AddWithValue("@dateupdated", DateTime.Now);
            }
            else if (action == "delete")
            {
                resultActionMessage = action;
                cmd.CommandText = "UPDATE `stories` SET `isdeleted`=1, `deletedby`=@deletedby,`datedeleted`=@datedeleted WHERE `id` = @id";
                cmd.Parameters.AddWithValue("@id", model.ID);
                cmd.Parameters.AddWithValue("@deletedby", model.UpdatedBy);
                cmd.Parameters.AddWithValue("@datedeleted", DateTime.Now);
            }
            else
            {
                cmd.CommandText = "INSERT INTO `stories`(`title`, `content`, `classid`, `addedby`, `dateadded`, `updatedby`, `dateupdated`, `isdeleted`) VALUES (@title,@content,@classid,@addedby,@dateadded,@updatedby,@dateupdated,0)";
                cmd.Parameters.AddWithValue("@title", model.Title);
                cmd.Parameters.AddWithValue("@content", model.Content);
                cmd.Parameters.AddWithValue("@classid", model.ClassID);
                cmd.Parameters.AddWithValue("@addedby", model.UpdatedBy);
                cmd.Parameters.AddWithValue("@dateadded", DateTime.Now);
                cmd.Parameters.AddWithValue("@updatedby", model.UpdatedBy);
                cmd.Parameters.AddWithValue("@dateupdated", DateTime.Now);
            }

            try
            {
                con.Open();
                count = cmd.ExecuteNonQuery();
                resultModel.success = count > 0;
                resultModel.message = count > 0 ? $"Story successfully {resultActionMessage}d." : $"Failed to {resultActionMessage} story. Please check your data.";
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message; 
            }
            finally
            { 
                con.Close();
            }
            return resultModel;
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
                    model.ClassID = rd["classid"] != null && rd["classid"].ToString() != "" ? Convert.ToInt32(rd["classid"].ToString()) : 0;
                    model.AddedBy = rd["addedby"] != null && rd["addedby"].ToString() != "" ? Convert.ToInt32(rd["addedby"].ToString()) : 0;
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

        private List<StudentSubmissionModel> GetStudentSubmissions(int storyid)
        {
            List<StudentSubmissionModel> list = new List<StudentSubmissionModel>();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = @"SELECT stud.StoryTitle, stud.StudentName
	                                , CAST(((CASE WHEN stud.CntTotal = 0 THEN 0 ELSE ((CAST(stud.CntCorrect AS DECIMAL(10,2)))/(CAST(stud.CntTotal AS DECIMAL(10,2)))) END) * 100.0) AS DECIMAL(10,2)) as ResultPercentage 
                                FROM (
                                    SELECT s.title as StoryTitle, u.name as StudentName
    	                                , SUM(case when a.iscorrect is null or a.iscorrect = 0 then 0 else 1 end) as CntCorrect
                                        , (select COUNT(*) from `questions` gg where (gg.isdeleted is null or gg.isdeleted = 0) and gg.courseid = g.storyid) as CntTotal 
                                    FROM `grades` g INNER JOIN `answers` a on g.stud_answerid = a.id 
                                    left join stories s on s.id = g.storyid
                                    left join users u on u.id = g.studentid
                                    WHERE g.storyid = @storyid
                                    GROUP BY s.title, u.name
                                ) stud;";
            cmd.Parameters.AddWithValue("@storyid", storyid);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    StudentSubmissionModel model = new StudentSubmissionModel();
                    model.StoryTitle = rd["StoryTitle"] != null ? rd["StoryTitle"].ToString() : "";
                    model.StudentName = rd["StudentName"] != null ? rd["StudentName"].ToString() : "";
                    model.ResultPercentage = rd["ResultPercentage"] != null && rd["ResultPercentage"].ToString() != "" ? Convert.ToDecimal(rd["ResultPercentage"].ToString()) : 0;
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

        #endregion Stories


        #region Questions and Answers
        public ActionResult Questions(int? id)
        {
            if (Session["UserName"] == null)
            {
                return RedirectToAction("Login");
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
        public JsonResult SaveQuestionAndOptions(QuestionAndAnswerModel model, string action)
        {
            int count = 0;
            ResultQuestionUpdateModel result = new ResultQuestionUpdateModel();
            QuestionModel questionModel = new QuestionModel();
            questionModel.Question = model.Question;
            questionModel.StoryID = model.StoryID;
            questionModel.UpdatedBy = (int)Session["UserID"];
            ResultModel resultQuestionModel = SaveQuestionDB(questionModel, action);

            if(resultQuestionModel != null)
            {
                count++;
                for (int i = 1; i <= 4; i++)
                {
                    AnswerModel answerModel1 = new AnswerModel();
                    answerModel1.QuestionID = resultQuestionModel.id; 
                    answerModel1.Sequence = i;
                    answerModel1.IsCorrect = model.CorrectAnswer == i;

                    if(i == 1)
                        answerModel1.Option = model.Option1;
                    if (i == 2)
                        answerModel1.Option = model.Option2;
                    if (i == 3)
                        answerModel1.Option = model.Option3;
                    if (i == 4)
                        answerModel1.Option = model.Option4;

                    ResultModel resultAnswerModel1 = SaveAnswerDB(answerModel1, action);
                    if(resultAnswerModel1.success) count++;
                }
                 
                result.success = true;
                result.message = "Successfully saved data.";
                result.question = GetQuestionsAndAnswersByQuestionID(resultQuestionModel.id);
            }

            return Json(result);
        }

        [HttpPost]
        public JsonResult SaveQuestion(QuestionModel model, string action)
        {
            ResultModel resultModel = new ResultModel();
            try
            {
                model.UpdatedBy = (int)Session["UserID"];
                resultModel = SaveQuestionDB(model, action);
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message;
                return Json(resultModel);
            }
            return Json(resultModel);
        }

        [HttpPost]
        public JsonResult SaveQuestionOrAnswer(int questionid, int answerid, string edit_text, bool iscorrect, string edit)
        {
            ResultModel resultModel = new ResultModel();
            try
            {
                if (edit == "question")
                {
                    QuestionModel model = new QuestionModel();
                    model.ID = questionid;
                    model.Question = edit_text;
                    model.UpdatedBy = (int)Session["UserID"];
                    resultModel = SaveQuestionDB(model, "update");
                }
                else
                {
                    AnswerModel model = new AnswerModel();
                    model.ID = answerid;
                    model.QuestionID = questionid;
                    model.Option = edit_text;
                    model.IsCorrect = iscorrect;
                    resultModel = SaveAnswerDB(model, "update");
                }
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message;
                return Json(resultModel);
            }
            return Json(resultModel);
        }

        [HttpPost]
        public JsonResult UpdateAnswerSorting(string[] order)
        {
            ResultModel res = new ResultModel();
            var ansIDs = order.Select(id => int.Parse(id.Replace("ans-", ""))).ToList();

            for (int i = 0; i < ansIDs.Count; i++)
            {
                AnswerModel model = new AnswerModel();
                model.ID = ansIDs[i];
                model.Sequence = i + 1; 
                res = SaveAnswerDB(model, "updateSortingOnly");
            }
            return Json(res);
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

        private QuestionModel GetQuestionsAndAnswersByQuestionID(int id)
        {
            QuestionModel question = new QuestionModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();
            cmd.CommandText = "select * from questions where id = @id;";
            cmd.Parameters.AddWithValue("@id", id);

            try
            {
                con.Open();
                MySqlDataReader rd = cmd.ExecuteReader();
                if (rd.Read())
                { 
                    question.ID = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                    question.Question = rd["question"] != null ? rd["question"].ToString() : "";
                    question.AddedBy = rd["addedby"] != null && rd["dateadded"].ToString() != "" ? Convert.ToInt32(rd["addedby"].ToString()) : 0;
                    question.DateAdded = rd["dateadded"] != null && rd["dateadded"].ToString() != "" ? Convert.ToDateTime(rd["dateadded"].ToString()) : new DateTime(2000, 1, 1);
                    question.UpdatedBy = rd["updatedby"] != null && rd["updatedby"].ToString() != "" ? Convert.ToInt32(rd["updatedby"].ToString()) : 0;
                    question.DateUpdated = rd["dateupdated"] != null && rd["dateupdated"].ToString() != "" ? Convert.ToDateTime(rd["dateupdated"].ToString()) : new DateTime(2000, 1, 1);
                    question.Answers = GetAnswersByQuestionID(question.ID); 
                }
                rd.Close();
            }
            finally
            {
                con.Close();
            }
            return question;
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

        private ResultModel SaveQuestionDB(QuestionModel model, string action)
        {
            int count = 0;
            string resultActionMessage = "save";
            ResultModel resultModel = new ResultModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();

            if (action == "update")
            {
                cmd.CommandText = "UPDATE `questions` SET `question`=@question,`updatedby`=@updatedby,`dateupdated`=@dateupdated WHERE `id` = @id";
                cmd.Parameters.AddWithValue("@id", model.ID);
                cmd.Parameters.AddWithValue("@question", model.Question); 
                cmd.Parameters.AddWithValue("@updatedby", model.UpdatedBy);
                cmd.Parameters.AddWithValue("@dateupdated", DateTime.Now);
            }
            else if (action == "delete")
            {
                resultActionMessage = action;
                cmd.CommandText = @"DELETE FROM `answers` WHERE `questionid` = @id;
                                    UPDATE `questions` SET `isdeleted`=1, `deletedby`=@deletedby,`datedeleted`=@datedeleted WHERE `id` = @id;";
                cmd.Parameters.AddWithValue("@id", model.ID);
                cmd.Parameters.AddWithValue("@deletedby", model.UpdatedBy);
                cmd.Parameters.AddWithValue("@datedeleted", DateTime.Now);
            }
            else
            {
                cmd.CommandText = @"INSERT INTO `questions`(`question`, `courseid`, `addedby`, `dateadded`, `updatedby`, `dateupdated`) 
                                                    VALUES (@question, @courseid, @addedby, @dateadded, @updatedby, @dateupdated); 
                                    SELECT LAST_INSERT_ID() as id;";
                cmd.Parameters.AddWithValue("@question", model.Question);
                cmd.Parameters.AddWithValue("@courseid", model.StoryID);
                cmd.Parameters.AddWithValue("@addedby", model.UpdatedBy);
                cmd.Parameters.AddWithValue("@dateadded", DateTime.Now);
                cmd.Parameters.AddWithValue("@updatedby", model.UpdatedBy);
                cmd.Parameters.AddWithValue("@dateupdated", DateTime.Now);
            }

            try
            {
                con.Open();
                if (action == "add")
                {
                    MySqlDataReader rd = cmd.ExecuteReader();
                    if (rd.Read())
                    {
                        count++;
                        resultModel.id = rd["id"] != null && rd["id"].ToString() != "" ? Convert.ToInt32(rd["id"].ToString()) : 0;
                        rd.Close();
                    }
                }
                else
                {
                    count = cmd.ExecuteNonQuery();
                }
                resultModel.success = count > 0;
                resultModel.message = count > 0 ? $"Question successfully {resultActionMessage}d." : $"Failed to {resultActionMessage} question. Please check your data.";
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message;
            }
            finally
            {
                con.Close();
            }
            return resultModel;
        }

        private ResultModel SaveAnswerDB(AnswerModel model, string action)
        {
            int count = 0;
            string resultActionMessage = "save";
            ResultModel resultModel = new ResultModel();
            MySqlConnection con = new MySqlConnection(defaultConnection);
            MySqlCommand cmd = con.CreateCommand();

            if (action == "update")
            {
                if(model.IsCorrect)
                {
                    cmd.CommandText = @"UPDATE `answers` SET `iscorrect` = 0 WHERE `questionid` = @questionid;
                                        UPDATE `answers` SET `option`=@option,`iscorrect`=@iscorrect WHERE `id` = @id;";
                }
                else
                { 
                    cmd.CommandText = @"UPDATE `answers` SET `option`=@option WHERE `id` = @id;";
                } 
                cmd.Parameters.AddWithValue("@id", model.ID);
                cmd.Parameters.AddWithValue("@questionid", model.QuestionID);
                cmd.Parameters.AddWithValue("@option", model.Option);
                cmd.Parameters.AddWithValue("@iscorrect", model.IsCorrect); 
            }  
            else if (action == "updateSortingOnly")
            {
                cmd.CommandText = @"UPDATE `answers` SET `sequence`=@sequence WHERE `id` = @id;";
                cmd.Parameters.AddWithValue("@id", model.ID);
                cmd.Parameters.AddWithValue("@sequence", model.Sequence);
            }
            else
            {
                cmd.CommandText = @"INSERT INTO `answers`(`questionid`, `option`, `iscorrect`, `sequence`) VALUES (@questionid,@option,@iscorrect,@sequence)";
                cmd.Parameters.AddWithValue("@questionid", model.QuestionID);
                cmd.Parameters.AddWithValue("@option", model.Option);
                cmd.Parameters.AddWithValue("@iscorrect", model.IsCorrect);
                cmd.Parameters.AddWithValue("@sequence", model.Sequence); 
            }

            try
            {
                con.Open();
                count = cmd.ExecuteNonQuery();
                resultModel.success = count > 0;
                resultModel.message = count > 0 ? $"Anwer successfully {resultActionMessage}d." : $"Failed to {resultActionMessage} answer. Please check your data.";
            }
            catch (Exception ex)
            {
                resultModel.success = false;
                resultModel.message = ex.Message;
            }
            finally
            {
                con.Close();
            }
            return resultModel;
        }


        #endregion Questions and Answers


         

    }
}