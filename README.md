# 帧同步+状态同步实现多人联机射击游戏
wasd移动，r换弹，右键瞄准后左键射击，tab查看计分板

使用Server场景创建房间，使用Game场景加入房间

已知bug:瞄准时视角与预期不符
自己实现的2d碰撞检测，靠近墙边角可能出现抖动或顿卡。

使用socket实现帧同步+状态同步结合，新玩家中途加入时服务器发送状态，其余时间发送操作，命中判定在客户端。

主题框架参考:<https://github.com/k8w/tsrpc-examples/tree/main/examples/cocos-creator-multiplayer>
