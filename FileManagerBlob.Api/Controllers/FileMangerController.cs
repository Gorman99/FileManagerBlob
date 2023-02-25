﻿using FileManagerBlob.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FileManagerBlob.Api.Controllers;


[ApiController]
[Route("[controller]")]
public class FileMangerController : ControllerBase
{

    private readonly IFileMangerService _fileMangerService;
    public FileMangerController(IFileMangerService fileMangerService)
    {
        _fileMangerService = fileMangerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        var result = await _fileMangerService.ListAsync();

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> UploadFiles(IFormFile file)
    {
        var result = await _fileMangerService.UploadAsync(file);

        if (result.Error == true)
        {
            // We got an error during upload, return an error with details to the client
            return StatusCode(StatusCodes.Status500InternalServerError, result.Status);
        }
        else
        {
            // Return a success message to the client about successfull upload
            return StatusCode(StatusCodes.Status200OK, result);
        }
    }
}