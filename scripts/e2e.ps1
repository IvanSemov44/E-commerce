$base = 'http://localhost:5000'

# Register
$regBody = @{ Email='e2e+user+1@example.com'; Password='TestP@ssw0rd!'; FirstName='E2E'; LastName='User' } | ConvertTo-Json
$reg = Invoke-RestMethod -Uri "$base/api/auth/register" -Method Post -Body $regBody -ContentType 'application/json'
Write-Output ("REGISTER_OK: " + ($reg | ConvertTo-Json -Compress))

# Login
$loginBody = @{ Email='e2e+user+1@example.com'; Password='TestP@ssw0rd!' } | ConvertTo-Json
$login = Invoke-RestMethod -Uri "$base/api/auth/login" -Method Post -Body $loginBody -ContentType 'application/json'
$token = $login.data.token
Write-Output ("TOKEN_LEN: " + $token.Length)
[Environment]::SetEnvironmentVariable('E2E_TOKEN',$token,'Process')

# Get products
$products = Invoke-RestMethod -Uri "$base/api/products" -Method Get
$product = $products.data.items[0]
Write-Output ("PRODUCT_CHOSEN: " + $product.id + " " + $product.name + " $" + $product.price)
[Environment]::SetEnvironmentVariable('E2E_PID',$product.id,'Process')
[Environment]::SetEnvironmentVariable('E2E_PNAME',$product.name,'Process')
[Environment]::SetEnvironmentVariable('E2E_PPRICE',$product.price,'Process')

# Add to cart
$prodId = $env:E2E_PID
$token = $env:E2E_TOKEN
$addBody = @{ productId = $prodId; quantity = 1 } | ConvertTo-Json
$add = Invoke-RestMethod -Uri "$base/api/cart/add-item" -Method Post -Body $addBody -ContentType 'application/json' -Headers @{ Authorization = "Bearer $token" }
Write-Output ("ADD_TO_CART_OK: " + ($add | ConvertTo-Json -Compress))

# Create order
$prodName = $env:E2E_PNAME
$prodPrice = $env:E2E_PPRICE
$createBody = @{ 
  Items = @(@{ ProductId = $prodId; ProductName = $prodName; Price = [decimal]$prodPrice; Quantity = 1; ImageUrl = $null })
  ShippingAddress = @{ FirstName='E2E'; LastName='User'; StreetLine1='123 Test St'; City='Testville'; State='TS'; PostalCode='12345'; Country='US'; Phone='555-0100' }
  PaymentMethod = 'card'
  PromoCode = $null
} | ConvertTo-Json -Depth 5
$order = Invoke-RestMethod -Uri "$base/api/orders" -Method Post -Body $createBody -ContentType 'application/json' -Headers @{ Authorization = "Bearer $token" }
Write-Output ("CREATE_ORDER_OK: " + ($order | ConvertTo-Json -Compress))
