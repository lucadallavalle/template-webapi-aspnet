using HumbleMediator;
using Microsoft.AspNetCore.Mvc;
using WebApiTemplate.Application.Customers;
using WebApiTemplate.Application.Customers.Commands;
using WebApiTemplate.Application.Customers.Queries;
using WebApiTemplate.Core;
using WebApiTemplate.Core.Common;
using WebApiTemplate.WebApi.Common;
using WebApiTemplate.WebApi.Customers.Requests;
using WebApiTemplate.WebApi.Customers.Responses;

namespace WebApiTemplate.WebApi.Customers;

/// <summary>
/// The controller for the Customer entity.
/// </summary>
/// <param name="mediator"></param>
public sealed class CustomersController(IMediator mediator) : AppControllerBase(mediator)
{
    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <param name="request">The data for the new entity.</param>
    /// <returns>A 201 Created response with the ID of the new entity.</returns>
    [HttpPost]
    [Route("")]
    public async Task<ActionResult<CustomerCreatedResponse>> Create(CreateCustomerRequest request)
    {
        var id = await _mediator.SendCommand<CreateCustomerCommand, int>(
            new CreateCustomerCommand(request.ToDomainEntity())
        );
        return CreatedAtAction(nameof(GetById), new { id }, new CustomerCreatedResponse(id));
    }

    /// <summary>
    /// Lists entities with search, sorting, and pagination.
    /// </summary>
    /// <param name="search">Free-text search term.</param>
    /// <param name="orderBy">Comma-separated sort fields; prefix a field with '-' for descending.</param>
    /// <param name="offset">Zero-based offset.</param>
    /// <param name="limit">Maximum number of items to return.</param>
    /// <returns>A 200 OK response with a page of entities and the total count.</returns>
    [HttpGet]
    [Route("")]
    public async Task<ActionResult<PagedResponse<CustomerDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? orderBy,
        [FromQuery] int offset = 0,
        [FromQuery] int limit = 25
    )
    {
        var result = await _mediator.SendQuery<ListCustomersQuery, PagedResult<CustomerDto>>(
            new ListCustomersQuery(search, orderBy, offset, limit)
        );

        return Ok(new PagedResponse<CustomerDto>(result.Items, result.Total, offset, limit));
    }

    /// <summary>
    /// Get an entity by its ID.
    /// </summary>
    /// <param name="id">The ID of the entity to get.</param>
    /// <returns>A 200 OK response with entity data, or a 404 Not Found response if the entity does not exist.</returns>
    [HttpGet]
    [Route("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id)
    {
        var result = await _mediator.SendQuery<GetCustomerByIdQuery, CustomerDto?>(
            new GetCustomerByIdQuery(id)
        );
        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates an entity.
    /// </summary>
    /// <param name="id">The id of the entity to update.</param>
    /// <param name="request">The new values for the entity.</param>
    /// <returns>A 204 No Content response.</returns>
    [HttpPut]
    [Route("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateCustomerRequest request)
    {
        await _mediator.SendCommand<UpdateCustomerCommand, Nothing>(
            new UpdateCustomerCommand(id, request.ToDomainEntity())
        );
        return NoContent();
    }

    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    /// <param name="id">The id of the entity to delete.</param>
    /// <returns>A 204 No Content response.</returns>
    [HttpDelete]
    [Route("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.SendCommand<DeleteCustomerCommand, Nothing>(new DeleteCustomerCommand(id));
        return NoContent();
    }
}
