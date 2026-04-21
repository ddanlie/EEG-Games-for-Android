from fastapi import APIRouter, HTTPException,  Response, status, Depends
from fastapi.responses import JSONResponse
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from pydantic import BaseModel
from models import (
    UserIdentity,
    EmailRequest,
    EmailCodeRequest,
)
from appservice import (
    app_login_request,
    app_login,
    app_login_token
)


router = APIRouter(prefix="/api/v1")
security = HTTPBearer()


@router.post("/auth/login/request", status_code=status.HTTP_204_NO_CONTENT)
def login_request(erequest: EmailRequest):
    if(app_login_request(erequest)):
        return Response(status_code=status.HTTP_204_NO_CONTENT)
    raise HTTPException(status_code=404, detail="Failed sending the code") 


@router.post("/auth/login", response_model=UserIdentity)
def login_first_time(eqrequest: EmailCodeRequest):
    user = app_login(eqrequest)
    if user is None:
        raise HTTPException(status_code=404, detail="Login failed, likely the wrong code from email pasted")
    return user

@router.post("/auth/login", response_model=UserIdentity)
def login_with_token(
    credentials: HTTPAuthorizationCredentials = Depends(security)
):
    token = credentials.credentials
    user = app_login_token(token)
    if user is None:
        raise HTTPException(status_code=403, detail="Failed to login, token might be expired or user does not exist")
    return user





# # --- Routes ---

# @router.get("/items", response_model=list[ItemResponse])
# def get_items():
#     """Return all items."""
#     return [
#         {"id": 1, "name": "Example item", "description": "A sample item"},
#     ]


# @router.get("/items/{item_id}", response_model=ItemResponse)
# def get_item(item_id: int):
#     """Return a single item by ID."""
#     if item_id != 1:
#         raise HTTPException(status_code=404, detail="Item not found")
#     return {"id": item_id, "name": "Example item", "description": "A sample item"}


# @router.post("/items", response_model=ItemResponse, status_code=201)
# def create_item(item: Item):
#     """Create a new item."""
#     return {"id": 2, **item.model_dump()}


# @router.delete("/items/{item_id}", status_code=204)
# def delete_item(item_id: int):
#     """Delete an item by ID."""
#     if item_id != 1:
#         raise HTTPException(status_code=404, detail="Item not found")