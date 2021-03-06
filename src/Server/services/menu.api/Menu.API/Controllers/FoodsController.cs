using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Menu.API.Abstraction.Managers;
using Menu.API.Abstraction.Repositories;
using Menu.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Menu.API.Controllers
{
    [Produces("application/json")]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class FoodsController : Controller
    {
        private readonly IFileUploadManager _fileUploadManager;
        private readonly IMapper _mapper;
        private readonly IRepository<Food> _repository;

        public FoodsController(
            IMapper mapper,
            IRepository<Food> repository,
            IFileUploadManager fileUploadManager)
        {
            _mapper = mapper;
            _repository = repository;
            _fileUploadManager = fileUploadManager;
        }

        [HttpGet("{count?}/{skip?}")]
        public IEnumerable<FoodDto> Get(int? count = 10, int? skip = 0)
        {
            var entities = _repository.GetAll()
                .Skip(skip.Value)
                .Take(count.Value)
                .ToList();

            return _mapper.Map<IEnumerable<FoodDto>>(entities);
        }

        [HttpGet("{id}")]
        public FoodDto Get(Guid id)
        {
            return _mapper.Map<FoodDto>(_repository.Get(id));
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] FoodDto foodDto)
        {
            try
            {
                var food = _mapper.Map<Food>(foodDto);
                food.Picture = _fileUploadManager.GetUploadedFileByUniqId(food.Id.ToString());
                _repository.Create(food);
                var result = await _repository.Commit();
                if (result)
                {
                    _fileUploadManager.Reset();
                    return Ok();
                }
                _fileUploadManager.RemoveUploadedFileByUniqId(foodDto.Id.ToString());
                return BadRequest();
            }
            catch (Exception)
            {
                _fileUploadManager.RemoveUploadedFileByUniqId(foodDto.Id.ToString());
                return BadRequest();
            }
        }

        [HttpPost]
        [Route("UploadFoodImage")]
        public async Task Post([Bind] IFormFile file, [Bind] string foodId)
        {
            await _fileUploadManager.Upload(file, foodId);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] FoodDto foodDto)
        {
            try
            {
                if (id != foodDto.Id)
                    return BadRequest();
                var food = _mapper.Map<Food>(foodDto);
                if (_fileUploadManager.HasFile(id.ToString()))
                {
                    _fileUploadManager.Remove(food.Picture);
                    food.Picture = _fileUploadManager.GetUploadedFileByUniqId(id.ToString());
                }

                _repository.Update(id, food);
                return await _repository.Commit() ? Ok() : (IActionResult)BadRequest();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var food = _repository.Get(id);
                _fileUploadManager.Remove(food.Picture);
                _repository.Delete(food);
                return await _repository.Commit() ? Ok() : (IActionResult)BadRequest();
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}