# Microsoft Bot Framework & LUIS를 활용한 챗봇 예제

단순한 rul base 형식으로 구현되어 있다. 아직 미완성.

## 기본구성
`LuisDialog`를 상속받은 `RootLuisDialog`와 또 `BasicLuisDialog : RootLuisDialog`로 기본 구성 되어있다. 사용자의 입력한 utterance는 BasicLuisDialog의 MessageReceived를 거쳐 BasicLuisDialog에서 LUIS intent에 맞게 분기된다. 거기에 더 해서 각각의 Action이 필요한 Dialog로 구성했다.
* 등록 : RegisterDialog
* 조회 : CheckDialog
* 취소 : CancelDialog

위와 같은 기능에 따른 다이얼로그를 구성하고 `BasicLuisDialog`에서 LUIS에 의해 파악한 의도 대로 알맞은 다이얼로그를 호출하게 된다.

```CSharp
 await context.Forward(new RegisterDialog(result, this.service), base.ResumeAfterCallback, new Activity { Text = result.Query }, CancellationToken.None);
```

## 시나리오 구성
예를 들어 휴가 등록을 위한 시나리오에서는 기본적으로 3가지가 필요했다.

1. 누가
2. 언제
3. 반차? 완차? 여부

이 3가지의 내용을 알고 있다면 휴가를 등록할 수 있다. 사용자의 Utterance에 이 3가지에 대한 내용이 없다면 그부분을 물어봐야 할 것이다. 1,2,3 번에 대한 값을 모두 채워야 휴가 등록을 할 수 있다.

#### 다이얼로그의 구성
다이얼로그에서는 `MessageReceivedAsync`를 계속 호출하는 방식이다. 흡사 재귀와 비슷한 형식이다. 이렇게 구성하는 이유는
1. 사용자의 입력에 대해 LUIS를 계속호출 해야한다.
2. Stack을 쌓으며 진행하면 중복되는 코드가 많아 진다.
간단히는 이 두가지 이유때문이다. 결국엔 똑같은 소스가 계속 반복되고 소스를 보기가 힘들어진다.

## 그외

#### 재활용성
만들고 구성을 하다보니 동일한 질문이 많았다. 등록할때도 누가, 언제에 대한 질문이 필요하고 조회, 취소할때도 동일하게 누가 언제에 대한 질문이 계속 필요했다. 때문에 질문 대해서도 분기하기로 했다. `AskDateDialog`와 같은 형식으로 날짜에 대한 값을 물어보고 처리하는 다이얼로그를 만들고 등록, 조회, 취소에 필요한 자리에 호출을 하면 날짜에 대한 질문을 하고 답을 얻어오는 식으로 구성했다.

#### Context
Dialog의 시나리오를 생성하면 context를 항상 달고 다니게된다. 때문에 흐름을 기억하는 경우와 같이 값을 기억해야할때 Context에 값을 넣어두면 Dialog를 옮겨가는것에 상관없이 값을 계속 유지 할 수 있따.

##### Context.data 사용하기.

```CSharp
//입력
context.ConversationData.SetValue("meetingRoom", meetingRoom);
context.ConversationData.SetValue("time", time);

//가져오기
context.ConversationData.TryGetValue("RoomReserved", out reservations)
```

## 문제점.
### 자연어 처리(한글) 어려움
#### Bot Framework의 한글지원.
영어와 한글에서 제공하는 prebuilt가 없다보니 다 구현해줘야하는 부분이 있다. 예를 들어 날짜의 경우 다음날,  이틀뒤, 어제, 오늘, 엊그제, 다음주 일요일 등등 이런 언어 대한 처리를 다 해줘야 한다. 필요한 경우 하드하게 코딩을 해야하는 부분이 있다.
결국 더 완벽한 처리를 위해서는 한글 자연어 처리에 대한 이해가 필요하다.

#### 생각지 못한 입력에 대응하기.
챗봇을 만들때는 범위를 축소하는게 중요하다. 첫번째는 시나리오는 다양하고 촘촘하게 구성하는게 우선이겠다. 지금 구성은 휴가 관리에 대한 매우 한정된 영역만 처리할 수 있다. 그러나 사람들은 AI 챗봇의 사용을 기대하면 영화에서 보던것 같은 높은 수준을 기대한다. 그래서 다양하게 입력을 해보고 반복되는 답변과 척척 알아듣지 못하는 반응에 이내 실망하고 만다. 어떻게 하면 사용자가 마치 AI처럼 느낄 수 있게. 이것 저것 잘 대응 할 수 있을까? 그럴려면 디자인을 잘해야되는데 모든 부분에 시나리오를 만든다는건 불가능하다. 좀 더 문장을 잘 파악하고 유연한 대처를 위한 연구가 필요하다.
지속적인 업데이트 학습은 필수이며, 자연어 처리에 대한 높은 이해도가 필요하다. 이부분은 더 연구 필요.
