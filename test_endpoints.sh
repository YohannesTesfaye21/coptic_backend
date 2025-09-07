#!/bin/bash

# Test script for Coptic App Backend API
# Base URL
BASE_URL="http://162.243.165.212:5000"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Testing Coptic App Backend API Endpoints${NC}"
echo "=================================================="

# Test 1: Login as Abune
echo -e "\n${YELLOW}1. Testing Abune Login${NC}"
ABUNE_TOKEN=$(curl -s -X POST "$BASE_URL/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "abune@church.com",
    "password": "abune123"
  }' | jq -r '.token')

if [ "$ABUNE_TOKEN" != "null" ] && [ -n "$ABUNE_TOKEN" ]; then
    echo -e "${GREEN}✅ Abune login successful${NC}"
    echo "Token: ${ABUNE_TOKEN:0:50}..."
else
    echo -e "${RED}❌ Abune login failed${NC}"
    exit 1
fi

# Test 2: Login as Regular User
echo -e "\n${YELLOW}2. Testing Regular User Login${NC}"
USER_TOKEN=$(curl -s -X POST "$BASE_URL/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "joguyjohna@gmail.com",
    "password": "Pass@123"
  }' | jq -r '.token')

if [ "$USER_TOKEN" != "null" ] && [ -n "$USER_TOKEN" ]; then
    echo -e "${GREEN}✅ Regular user login successful${NC}"
    echo "Token: ${USER_TOKEN:0:50}..."
else
    echo -e "${RED}❌ Regular user login failed${NC}"
    exit 1
fi

# Test 3: Get Conversations (Abune)
echo -e "\n${YELLOW}3. Testing Get Conversations (Abune)${NC}"
CONVERSATIONS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/Chat/conversations" \
  -H "Authorization: Bearer $ABUNE_TOKEN" \
  -H "accept: */*")

if echo "$CONVERSATIONS_RESPONSE" | jq -e '.conversations' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Get conversations (Abune) successful${NC}"
    echo "Response: $(echo "$CONVERSATIONS_RESPONSE" | jq '.conversations | length') conversations found"
else
    echo -e "${RED}❌ Get conversations (Abune) failed${NC}"
    echo "Response: $CONVERSATIONS_RESPONSE"
fi

# Test 4: Get Conversations (Regular User)
echo -e "\n${YELLOW}4. Testing Get Conversations (Regular User)${NC}"
USER_CONVERSATIONS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/Chat/conversations" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "accept: */*")

if echo "$USER_CONVERSATIONS_RESPONSE" | jq -e '.conversations' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Get conversations (Regular User) successful${NC}"
    echo "Response: $(echo "$USER_CONVERSATIONS_RESPONSE" | jq '.conversations | length') conversations found"
else
    echo -e "${RED}❌ Get conversations (Regular User) failed${NC}"
    echo "Response: $USER_CONVERSATIONS_RESPONSE"
fi

# Test 5: Send Message (Abune to User)
echo -e "\n${YELLOW}5. Testing Send Message (Abune to User)${NC}"
SEND_MESSAGE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/Chat/send" \
  -H "Authorization: Bearer $ABUNE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientId": "75fc4834-5921-41b7-94f6-07b2c30b1fd3",
    "content": "Hello from Abune! This is a test message.",
    "messageType": 0
  }')

if echo "$SEND_MESSAGE_RESPONSE" | jq -e '.id' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Send message (Abune to User) successful${NC}"
    MESSAGE_ID=$(echo "$SEND_MESSAGE_RESPONSE" | jq -r '.id')
    echo "Message ID: $MESSAGE_ID"
else
    echo -e "${RED}❌ Send message (Abune to User) failed${NC}"
    echo "Response: $SEND_MESSAGE_RESPONSE"
fi

# Test 6: Send Message (User to Abune)
echo -e "\n${YELLOW}6. Testing Send Message (User to Abune)${NC}"
USER_SEND_MESSAGE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/Chat/send" \
  -H "Authorization: Bearer $USER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientId": "96adbd2a-c124-4061-8897-0f9c9eb0e1df",
    "content": "Hello from User! This is a reply message.",
    "messageType": 0
  }')

if echo "$USER_SEND_MESSAGE_RESPONSE" | jq -e '.id' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Send message (User to Abune) successful${NC}"
    USER_MESSAGE_ID=$(echo "$USER_SEND_MESSAGE_RESPONSE" | jq -r '.id')
    echo "Message ID: $USER_MESSAGE_ID"
else
    echo -e "${RED}❌ Send message (User to Abune) failed${NC}"
    echo "Response: $USER_SEND_MESSAGE_RESPONSE"
fi

# Test 7: Get Messages
echo -e "\n${YELLOW}7. Testing Get Messages${NC}"
GET_MESSAGES_RESPONSE=$(curl -s -X GET "$BASE_URL/api/Chat/messages?conversationId=default-conversation&page=1&pageSize=10" \
  -H "Authorization: Bearer $ABUNE_TOKEN" \
  -H "accept: */*")

if echo "$GET_MESSAGES_RESPONSE" | jq -e '.[]' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Get messages successful${NC}"
    echo "Response: $(echo "$GET_MESSAGES_RESPONSE" | jq 'length') messages found"
else
    echo -e "${RED}❌ Get messages failed${NC}"
    echo "Response: $GET_MESSAGES_RESPONSE"
fi

# Test 8: Get Unread Counts
echo -e "\n${YELLOW}8. Testing Get Unread Counts${NC}"
UNREAD_COUNTS_RESPONSE=$(curl -s -X GET "$BASE_URL/api/Chat/unread-counts" \
  -H "Authorization: Bearer $ABUNE_TOKEN" \
  -H "accept: */*")

if echo "$UNREAD_COUNTS_RESPONSE" | jq -e '.' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Get unread counts successful${NC}"
    echo "Response: $UNREAD_COUNTS_RESPONSE"
else
    echo -e "${RED}❌ Get unread counts failed${NC}"
    echo "Response: $UNREAD_COUNTS_RESPONSE"
fi

# Test 9: Get Community Messages
echo -e "\n${YELLOW}9. Testing Get Community Messages${NC}"
COMMUNITY_MESSAGES_RESPONSE=$(curl -s -X GET "$BASE_URL/api/Chat/community" \
  -H "Authorization: Bearer $ABUNE_TOKEN" \
  -H "accept: */*")

if echo "$COMMUNITY_MESSAGES_RESPONSE" | jq -e '.messages' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Get community messages successful${NC}"
    echo "Response: $(echo "$COMMUNITY_MESSAGES_RESPONSE" | jq '.messages | length') messages found"
else
    echo -e "${RED}❌ Get community messages failed${NC}"
    echo "Response: $COMMUNITY_MESSAGES_RESPONSE"
fi

# Test 10: Get Broadcast Messages
echo -e "\n${YELLOW}10. Testing Get Broadcast Messages${NC}"
BROADCAST_MESSAGES_RESPONSE=$(curl -s -X GET "$BASE_URL/api/Chat/broadcast" \
  -H "Authorization: Bearer $ABUNE_TOKEN" \
  -H "accept: */*")

if echo "$BROADCAST_MESSAGES_RESPONSE" | jq -e '.messages' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Get broadcast messages successful${NC}"
    echo "Response: $(echo "$BROADCAST_MESSAGES_RESPONSE" | jq '.messages | length') messages found"
else
    echo -e "${RED}❌ Get broadcast messages failed${NC}"
    echo "Response: $BROADCAST_MESSAGES_RESPONSE"
fi

# Test 11: Send Broadcast Message
echo -e "\n${YELLOW}11. Testing Send Broadcast Message${NC}"
BROADCAST_SEND_RESPONSE=$(curl -s -X POST "$BASE_URL/api/Chat/broadcast" \
  -H "Authorization: Bearer $ABUNE_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "This is a broadcast message to all community members!",
    "messageType": 0
  }')

if echo "$BROADCAST_SEND_RESPONSE" | jq -e '.id' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Send broadcast message successful${NC}"
    echo "Message ID: $(echo "$BROADCAST_SEND_RESPONSE" | jq -r '.id')"
else
    echo -e "${RED}❌ Send broadcast message failed${NC}"
    echo "Response: $BROADCAST_SEND_RESPONSE"
fi

# Test 12: File Upload (if message ID exists)
if [ -n "$MESSAGE_ID" ]; then
    echo -e "\n${YELLOW}12. Testing File Upload${NC}"
    FILE_UPLOAD_RESPONSE=$(curl -s -X POST "$BASE_URL/api/FileUpload/upload" \
      -H "Authorization: Bearer $ABUNE_TOKEN" \
      -F "recipientId=75fc4834-5921-41b7-94f6-07b2c30b1fd3" \
      -F "content=File upload test" \
      -F "messageType=1" \
      -F "file=@test_image.jpg")

    if echo "$FILE_UPLOAD_RESPONSE" | jq -e '.messageId' > /dev/null 2>&1; then
        echo -e "${GREEN}✅ File upload successful${NC}"
        echo "Response: $FILE_UPLOAD_RESPONSE"
    else
        echo -e "${RED}❌ File upload failed${NC}"
        echo "Response: $FILE_UPLOAD_RESPONSE"
    fi
else
    echo -e "\n${YELLOW}12. Skipping File Upload (no message ID)${NC}"
fi

# Test 13: Search Messages
echo -e "\n${YELLOW}13. Testing Search Messages${NC}"
SEARCH_RESPONSE=$(curl -s -X GET "$BASE_URL/api/Chat/search?query=test&page=1&pageSize=10" \
  -H "Authorization: Bearer $ABUNE_TOKEN" \
  -H "accept: */*")

if echo "$SEARCH_RESPONSE" | jq -e '.messages' > /dev/null 2>&1; then
    echo -e "${GREEN}✅ Search messages successful${NC}"
    echo "Response: $(echo "$SEARCH_RESPONSE" | jq '.messages | length') messages found"
else
    echo -e "${RED}❌ Search messages failed${NC}"
    echo "Response: $SEARCH_RESPONSE"
fi

# Test 14: Edit Message (if message ID exists)
if [ -n "$MESSAGE_ID" ]; then
    echo -e "\n${YELLOW}14. Testing Edit Message${NC}"
    EDIT_RESPONSE=$(curl -s -X PUT "$BASE_URL/api/Chat/$MESSAGE_ID/edit" \
      -H "Authorization: Bearer $ABUNE_TOKEN" \
      -H "Content-Type: application/json" \
      -d '{
        "content": "This message has been edited!"
      }')

    if echo "$EDIT_RESPONSE" | jq -e '.id' > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Edit message successful${NC}"
        echo "Response: $EDIT_RESPONSE"
    else
        echo -e "${RED}❌ Edit message failed${NC}"
        echo "Response: $EDIT_RESPONSE"
    fi
else
    echo -e "\n${YELLOW}14. Skipping Edit Message (no message ID)${NC}"
fi

# Test 15: Delete Message (if message ID exists)
if [ -n "$MESSAGE_ID" ]; then
    echo -e "\n${YELLOW}15. Testing Delete Message${NC}"
    DELETE_RESPONSE=$(curl -s -X DELETE "$BASE_URL/api/Chat/$MESSAGE_ID" \
      -H "Authorization: Bearer $ABUNE_TOKEN")

    if echo "$DELETE_RESPONSE" | jq -e '.success' > /dev/null 2>&1; then
        echo -e "${GREEN}✅ Delete message successful${NC}"
        echo "Response: $DELETE_RESPONSE"
    else
        echo -e "${RED}❌ Delete message failed${NC}"
        echo "Response: $DELETE_RESPONSE"
    fi
else
    echo -e "\n${YELLOW}15. Skipping Delete Message (no message ID)${NC}"
fi

echo -e "\n${BLUE}🎯 API Testing Complete!${NC}"
echo "=================================================="
