using aiProj.Services;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace aiProj.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectController : Controller
    {
        private readonly string _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedDatasets");
        private ProjectService _projectService;

        public ProjectController(ProjectService projectService)
        {
            _projectService = projectService;
            // Создаем директорию для загрузки файлов, если её нет
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateProject([FromForm] ProjectViewModel project)
        {
            if (project == null)
            {
                return BadRequest("Данные проекта не были переданы.");
            }

            // Проверяем, есть ли файл
            if (project.File == null || project.File.Length == 0)
            {
                return BadRequest("Файл не был загружен.");
            }

            // Генерируем уникальное имя для файла
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + project.File.FileName;
            string filePath = Path.Combine(_storagePath, uniqueFileName);

            // Сохраняем файл на диск
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await project.File.CopyToAsync(fileStream);
            }

            // Создаем объект Project с информацией из ViewModel
            var proj = new Project()
            {
                Name = project.Name,
                UserLogin = User.Identity.Name,
                Type = project.Type,
                TargetColumn = project.TargetColumn,
                FilePath = filePath, // Сохраняем путь к файлу
                Params = "sdwfe" // Сериализуем Params в JSON-строку
            };

            // Сохраняем проект через сервис
            await _projectService.CreateProject(proj);

            return Ok(new { Message = "Проект успешно создан", FilePath = filePath });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetProjects()
        {
            var userName = User.Identity.Name;
            var result = await _projectService.GetAllUserProjects(userName);

            return Ok(result);
        }

        [HttpGet("{name}")]
        [Authorize]
        public async Task<IActionResult> GetProject(string name)
        {
            var userName = User.Identity.Name;
            var result = _projectService.GetAllUserProjects(userName).Result.First(x => x.Name == name);

            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{name}")]
        public async Task<IActionResult> DeleteProject(string name)
        {
            if (!await _projectService.DeleteProjectByName(name))
            {
                return BadRequest();
            }

            return Ok();
        }

        //[Authorize]
        //public async Task<IActionResult> UpdateProject([FromForm] ProjectViewModel project)
        //{
        //    string uniqueFileName = Guid.NewGuid().ToString() + "_" + project.File.FileName;
        //    string filePath = Path.Combine(_storagePath, uniqueFileName);

        //    // Сохраняем файл на диск
        //    using (var fileStream = new FileStream(filePath, FileMode.Create))
        //    {
        //        await project.File.CopyToAsync(fileStream);
        //    }


        //    var proj = new Project()
        //    {
        //        Name = project.Name,
        //        UserLogin = User.Identity.Name,
        //        Type = project.Type,
        //        TargetColumn = project.TargetColumn,
        //        FilePath = filePath, // Сохраняем путь к файлу
        //        Params = "sdwfe" // Сериализуем Params в JSON-строку
        //    };

        //    await _projectService.UpdateProject(proj);

        //    return Ok();
        //}

        [Authorize]
        [HttpPatch]
        public async Task<IActionResult> TrainModel(string projectName)
        {
            var project = _projectService.GetAllUserProjects(User.Identity.Name).Result.First(x => x.Name == projectName);

            string pythonPath = "python"; // Укажите путь к Python, если требуется.
            string scriptPath = "train_model.py"; // Укажите путь к вашему Python-скрипту.

            string modelType = project.Type;
            string targetColumn = project.TargetColumn;
            string filePath = project.FilePath;
            string paramsJson = project.Params;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"{scriptPath} \"{modelType}\" \"{targetColumn}\" \"{filePath}\" \"{paramsJson.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processStartInfo })
            {
                process.Start();

                string result = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(errors))
                {
                    return BadRequest(errors);
                }

                return Ok(result);
            }
        }
    }
}
